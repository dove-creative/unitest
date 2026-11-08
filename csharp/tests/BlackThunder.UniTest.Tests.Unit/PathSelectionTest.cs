using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Unit
{
    public class PathSelectionTest
    {
        private class SelectionModel : Model
        {
            public List<int> SelectedIndices { get; } = new();
        }

        private class SelectionProject : Project<SelectionModel>
        {
            private readonly IReadOnlyList<string> _ids;

            public SelectionProject(IReadOnlyList<string> ids, ExecutionMode mode) : base(mode) => _ids = ids;

            public override IEnumerable<ILab<SelectionModel>> CreateLabs(SelectionModel model) =>
                _ids.Select((id, index) => new Lab<SelectionModel>(id, actor: (m, _) => m.SelectedIndices.Add(index)));
        }


        [Test]
        public void HashMatchesSharedVectors()
        {
            // PS01: Hash raw UTF-16 code units, including isolated surrogate values.
            using var fixture = OpenFixture();
            foreach (var vector in fixture.RootElement.GetProperty("hashes").EnumerateArray())
            {
                var history = vector.TryGetProperty("utf16", out var codeUnits)
                    ? new string(codeUnits.EnumerateArray().Select(unit => (char)unit.GetInt32()).ToArray())
                    : vector.GetProperty("history").GetString();
                var actual = history.GetDeterministicRandom(vector.GetProperty("seed").GetInt32());
                Assert.That(actual, Is.EqualTo(vector.GetProperty("expected").GetInt32()), vector.GetRawText());
            }
        }

        [Test]
        public void RandomMatchesSharedVectors()
        {
            // PS02: Cover signed boundaries and a draw that requires rejection sampling.
            using var fixture = OpenFixture();
            foreach (var vector in fixture.RootElement.GetProperty("ranges").EnumerateArray())
            {
                var actual = vector.GetProperty("history").GetString().GetDeterministicRandom(
                    vector.GetProperty("minInclusive").GetInt32(),
                    vector.GetProperty("maxExclusive").GetInt32(),
                    vector.GetProperty("seed").GetInt32());
                Assert.That(actual, Is.EqualTo(vector.GetProperty("expected").GetInt32()), vector.GetRawText());
            }
        }

        [Test]
        public void OrderingMatchesSharedVectors()
        {
            // PS03: Verify code-unit ordering and stable ties against the shared expectation.
            using var fixture = OpenFixture();
            foreach (var vector in fixture.RootElement.GetProperty("ordering").EnumerateArray())
            {
                var indices = ReadIds(vector).Select((id, index) => (id, index))
                    .OrderBy(candidate => candidate.id, StringComparer.Ordinal)
                    .Select(candidate => candidate.index);
                Assert.That(indices, Is.EqualTo(ReadExpectedIndices(vector)), vector.GetRawText());
            }
        }

        [TestCase(ExecutionMode.Inline)]
        [TestCase(ExecutionMode.ThreadPool)]
        public async Task ContinuousPathMatchesSharedVectors(ExecutionMode mode)
        {
            // PS04: Inspect the actual expanded path. The final stage tests all siblings.
            using var fixture = OpenFixture();
            foreach (var vector in fixture.RootElement.GetProperty("paths").EnumerateArray())
            {
                var depth = vector.GetProperty("depth").GetInt32();
                var seed = vector.GetProperty("seed").GetInt32();
                var ids = ReadIds(vector);
                var root = await new SelectionProject(ids, mode).ExecuteContinuously(depth, seed);
                var expanded = root;
                for (var level = 1; level < depth; level++)
                {
                    Assert.That(expanded.Afters.Count, Is.EqualTo(ids.Length), vector.GetRawText());
                    expanded = expanded.Afters.Single(child => child.Afters.Count > 0);
                }

                Assert.That(expanded.Afters.Count, Is.EqualTo(ids.Length), vector.GetRawText());
                Assert.That(expanded.Model.SelectedIndices, Is.EqualTo(ReadExpectedIndices(vector)),
                    vector.GetRawText());
            }
        }

        private static JsonDocument OpenFixture() => JsonDocument.Parse(File.ReadAllText(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Contracts", "selection.json")));

        private static string[] ReadIds(JsonElement vector) => vector.GetProperty("ids").EnumerateArray()
            .Select(id => id.GetString()).ToArray();

        private static int[] ReadExpectedIndices(JsonElement vector) =>
            vector.GetProperty("expectedIndices").EnumerateArray().Select(index => index.GetInt32()).ToArray();
    }
}
