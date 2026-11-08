using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackThunder.UniTest;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Recursion.UniTest
{
    public class CompositeLabTest
    {
        private const int ScenarioCount = 51;

        public class ScenarioModel : Model
        {
            public string CompletedScenario;
        }

        public class ProbeModel : Model
        {
            public List<string> Trace { get; } = new();
        }

        private class Settings
        {
            public int Layers = 4;
            public string FailingHook;
            public Exception Failure = new InvalidOperationException("Injected callback failure");
            public Exception ActorFailure;
            public int[] ExpectedAt = Array.Empty<int>();
            public Type ExpectedType = typeof(InvalidOperationException);
            public Type[] ExpectedByLayer = new Type[4];
            public bool ExtensionActorsThrow;
            public int UnsustainableAt = -1;
            public int TerminalAt = -1;
            public string TerminalPhase = "Template";
            public int[] Counts = new[] { -1, -1, -1, -1 };
            public int? InitialCount;
            public string ExpectedStop;
            public string ExpectedErrorStage;
            public Type ExpectedInnerType;
            public Type ExpectedMissingType;
            public bool ExpectedSustainable = true;
            public bool ExpectedContinuable = true;
            public int? ExpectedRemainingCount;
        }

        private class Scenario
        {
            public string Id { get; }
            public Action Verify { get; }

            public Scenario(string id, Action verify)
            {
                Id = id;
                Verify = verify;
            }
        }

        private class ScenarioProject : Project<ScenarioModel>
        {
            public ScenarioProject(ExecutionMode mode) : base(mode) { }

            public override IEnumerable<ILab<ScenarioModel>> CreateLabs(ScenarioModel model)
            {
                if (model.ExecutionCount > 0)
                    yield break;

                foreach (var scenario in CreateScenarios())
                {
                    yield return new Lab<ScenarioModel>(scenario.Id, actor: (m, _) =>
                    {
                        scenario.Verify();
                        m.CompletedScenario = scenario.Id;
                    });
                }
            }
        }


        // CL01-CL51: Every planned scenario runs in both scheduling modes, including expected target failures.
        [TestCase(ExecutionMode.ThreadPool)]
        [TestCase(ExecutionMode.Inline)]
        public async Task AllScenariosExecute(ExecutionMode mode)
        {
            var project = new ScenarioProject(mode);
            var result = await project.Execute(1, stopOnFailure: false).WaitAsync(TimeSpan.FromSeconds(15));
            var expectedIds = Enumerable.Range(1, ScenarioCount).Select(index => $"CL{index:00}").ToArray();

            Assert.That(project.ProcessCount, Is.EqualTo(ScenarioCount));
            Assert.That(result.Afters.Count, Is.EqualTo(ScenarioCount));
            Assert.That(result.Afters.Select(node => node.Id), Is.EquivalentTo(expectedIds));
            Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().ToJson());
            Assert.That(cancelled, Is.False);
            Assert.That(result.Afters.Select(node => node.Model.CompletedScenario), Is.EquivalentTo(expectedIds));
        }

        // CL03, CL16, CL41, CL45: Representative success, failure, termination, and persistent extension replay.
        [TestCase("CL03")]
        [TestCase("CL16")]
        [TestCase("CL41")]
        [TestCase("CL45")]
        public async Task SelectedScenarioReplays(string id)
        {
            var project = new ScenarioProject(ExecutionMode.Inline);
            var result = await project.Execute(id).WaitAsync(TimeSpan.FromSeconds(10));

            Assert.That(result.Afters.Count, Is.EqualTo(1));
            Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().ToJson());
            Assert.That(cancelled, Is.False);
            Assert.That(result.Afters.Single().Model.CompletedScenario, Is.EqualTo(id));
        }

        // CL01-CL44, CL46-CL51: Inputs and explicit expected outcomes form the finite scenario matrix.
        private static IEnumerable<Scenario> CreateScenarios()
        {
            var scenarios = new List<Scenario>();

            for (var layers = 2; layers <= 4; layers++)
                Add(new Settings { Layers = layers });

            for (var layer = 0; layer < 4; layer++)
                AddFailure($"Metadata{layer}", "Arrange");

            for (var layer = 0; layer < 4; layer++)
                AddFailure($"Arrange{layer}", "Arrange");

            AddFailure("Act0", "Act");

            for (var layer = 0; layer < 4; layer++)
                AddFailure($"Assert{layer}", "Assert");

            Add(new Settings { ExtensionActorsThrow = true });

            for (var layer = 0; layer < 4; layer++)
            {
                Add(new Settings
                {
                    ExpectedAt = new[] { layer },
                    ActorFailure = new InvalidOperationException("Expected actor failure")
                });
            }

            for (var layer = 0; layer < 4; layer++)
            {
                Add(new Settings
                {
                    ExpectedAt = new[] { layer },
                    ExpectedStop = "Act0",
                    ExpectedErrorStage = "Act",
                    ExpectedInnerType = typeof(MissingExpectedException),
                    ExpectedSustainable = false,
                    ExpectedContinuable = false
                });
            }

            for (var layer = 0; layer < 4; layer++)
            {
                Add(new Settings
                {
                    ExpectedAt = new[] { layer },
                    ActorFailure = new ArgumentException("Unexpected actor failure"),
                    ExpectedStop = "Act0",
                    ExpectedErrorStage = "Act",
                    ExpectedInnerType = typeof(ArgumentException),
                    ExpectedSustainable = false,
                    ExpectedContinuable = false
                });
            }

            foreach (var layer in new[] { 1, 3 })
            {
                Add(new Settings
                {
                    ExpectedAt = new[] { 0, layer },
                    ActorFailure = new InvalidOperationException("Shared expected actor failure")
                });
            }

            for (var layer = 0; layer < 4; layer++)
                Add(new Settings { UnsustainableAt = layer, ExpectedSustainable = false });

            foreach (var phase in new[] { "Template", "Arrange", "Assert" })
            {
                foreach (var layer in new[] { 0, 3 })
                {
                    Add(new Settings
                    {
                        TerminalAt = layer,
                        TerminalPhase = phase,
                        ExpectedSustainable = false,
                        ExpectedContinuable = false
                    });
                }
            }

            Add(new Settings
            {
                Counts = new[] { -1, 7, 3, 5 },
                ExpectedSustainable = false,
                ExpectedRemainingCount = 3
            });

            Add(new Settings
            {
                Counts = new[] { 6, 0, 4, -1 },
                ExpectedSustainable = false,
                ExpectedContinuable = false,
                ExpectedRemainingCount = 0
            });

            Add(new Settings
            {
                Counts = new[] { 5, 3, 8, -1 },
                InitialCount = 2,
                ExpectedSustainable = false,
                ExpectedRemainingCount = 1
            });

            scenarios.Add(new Scenario("CL45", VerifyPersistentExtensions));

            var invalid = typeof(InvalidOperationException);
            var argument = typeof(ArgumentException);
            var general = typeof(Exception);

            AddUnmet(new[] { invalid, argument, null, null }, new InvalidOperationException("Original"), argument);
            AddUnmet(new[] { general, null, null, null }, null, general);
            AddUnmet(new[] { invalid, general, null, null }, null, invalid);
            AddUnmet(new[] { null, invalid, argument, null }, new InvalidOperationException("Extension"), argument);
            AddUnmet(new[] { invalid, argument, null, null }, new ArgumentException("Extension"), invalid);

            Add(new Settings
            {
                ExpectedByLayer = new[] { general, invalid, null, null },
                ActorFailure = new InvalidOperationException("Base and derived expected actor failure")
            });

            return scenarios;

            void Add(Settings settings) =>
                scenarios.Add(new Scenario($"CL{scenarios.Count + 1:00}", () => ExecuteScenario(settings)));

            void AddUnmet(Type[] expectedByLayer, Exception actorFailure, Type missingType)
            {
                Add(new Settings
                {
                    ExpectedByLayer = expectedByLayer,
                    ActorFailure = actorFailure,
                    ExpectedStop = "Act0",
                    ExpectedErrorStage = "Act",
                    ExpectedInnerType = typeof(MissingExpectedException),
                    ExpectedMissingType = missingType,
                    ExpectedSustainable = false,
                    ExpectedContinuable = false
                });
            }

            void AddFailure(string hook, string stage)
            {
                Add(new Settings
                {
                    FailingHook = hook,
                    ExpectedStop = hook,
                    ExpectedErrorStage = stage,
                    ExpectedInnerType = typeof(InvalidOperationException),
                    ExpectedSustainable = false,
                    ExpectedContinuable = false
                });
            }
        }

        // CL01-CL44, CL46-CL51: Observe the composition through public APIs and compare with an independent trace.
        private static void ExecuteScenario(Settings settings)
        {
            var labs = Enumerable.Range(0, settings.Layers).Select(layer => CreateLayer(settings, layer)).ToArray();
            var composition = new CompositeLab<ProbeModel>(labs[0], labs[1]);

            for (var layer = 2; layer < labs.Length; layer++)
                composition = composition.Extend(labs[layer]);

            var model = new ProbeModel();
            var staleKey = new object();
            model.MetadataGroup.Add(staleKey, new SubjectMetadata("stale"));

            if (settings.InitialCount.HasValue)
                model.RemainingExecutionCount = settings.InitialCount.Value;

            composition.Execute(model, out var error);

            Assert.That(model.Trace, Is.EqualTo(ExpectedTrace(settings)), error?.ToString());
            Assert.That(model.MetadataGroup.ContainsKey(staleKey), Is.False);
            Assert.That(model.ExecutionCount, Is.EqualTo(1));
            Assert.That(composition.Id, Is.EqualTo(string.Join("__", labs.Reverse().Select(lab => lab.Id))));
            Assert.That(model.GetExecutionHistory(), Is.EqualTo(composition.Id));
            Assert.That(model.Sustainable, Is.EqualTo(settings.ExpectedSustainable));
            Assert.That(model.Continuable, Is.EqualTo(settings.ExpectedContinuable));
            Assert.That(model.RemainingExecutionCount, Is.EqualTo(settings.ExpectedRemainingCount));

            if (settings.ExpectedErrorStage == null)
            {
                Assert.That(error, Is.Null);
                Assert.That(model.MetadataGroup.Count, Is.EqualTo(settings.Layers));

                for (var layer = 0; layer < labs.Length; layer++)
                {
                    var metadata = model.MetadataGroup[labs[layer]];
                    Assert.That(metadata.Metadata, Is.EqualTo(layer));
                    Assert.That(metadata, Is.Not.SameAs(labs[layer].MetadataTemplate));
                }
            }
            else
            {
                Assert.That(error, Is.Not.Null);
                Assert.That(error.Message, Does.StartWith(settings.ExpectedErrorStage + " failed"));
                Assert.That(error.InnerException, Is.TypeOf(settings.ExpectedInnerType));

                if (error.InnerException is MissingExpectedException missing)
                {
                    Assert.That(
                        missing.ExpectedExceptionType,
                        Is.EqualTo(settings.ExpectedMissingType ?? settings.ExpectedType));
                    Assert.That(missing.InnerException, Is.SameAs(settings.ActorFailure));
                }
                else
                    Assert.That(error.InnerException, Is.SameAs(settings.ActorFailure ?? settings.Failure));
            }

            for (var layer = 0; layer < labs.Length; layer++)
            {
                var template = labs[layer].MetadataTemplate;
                Assert.That(template.Metadata, Is.Null);
                Assert.That(template.ToUnsustainable, Is.EqualTo(settings.UnsustainableAt == layer));
                Assert.That(
                    template.ToUncontinuable,
                    Is.EqualTo(settings.TerminalAt == layer && settings.TerminalPhase == "Template"));
                Assert.That(template.RemainingExecutionCount, Is.EqualTo(settings.Counts[layer]));
            }
        }

        private static Lab<ProbeModel> CreateLayer(Settings settings, int layer)
        {
            return new Lab<ProbeModel>(
                $"Layer{layer}",
                expectedExceptionType: settings.ExpectedAt.Contains(layer)
                    ? settings.ExpectedType
                    : settings.ExpectedByLayer[layer],
                toUnsustainable: settings.UnsustainableAt == layer,
                toUncontinuable: settings.TerminalAt == layer && settings.TerminalPhase == "Template",
                remainingExecutionCount: settings.Counts[layer])
            {
                SetMetadata = model =>
                {
                    Record(model, $"Metadata{layer}");
                    return layer;
                },
                Arranger = (model, metadata) =>
                {
                    Record(model, $"Arrange{layer}");
                    Assert.That(metadata.Metadata, Is.EqualTo(layer));

                    if (settings.TerminalAt == layer && settings.TerminalPhase == "Arrange")
                        metadata.ToUncontinuable = true;
                },
                Actor = (model, _) =>
                {
                    Record(model, $"Act{layer}");

                    if (layer > 0 && settings.ExtensionActorsThrow)
                        throw new InvalidOperationException("An extension actor must not run");

                    if (layer == 0 && settings.ActorFailure != null)
                        throw settings.ActorFailure;
                },
                Asserter = (model, metadata) =>
                {
                    Record(model, $"Assert{layer}");
                    Assert.That(
                        model.Continuable,
                        Is.True,
                        "Apply terminal restrictions after the current assertions.");
                    Assert.That(metadata.Metadata, Is.EqualTo(layer));

                    if (settings.TerminalAt == layer && settings.TerminalPhase == "Assert")
                        metadata.ToUncontinuable = true;
                }
            };

            void Record(ProbeModel model, string hook)
            {
                model.Trace.Add(hook);

                if (settings.FailingHook == hook)
                    throw settings.Failure;
            }
        }

        // The oracle follows the public ordering contract, never the composition's internal layer collection.
        private static IEnumerable<string> ExpectedTrace(Settings settings)
        {
            var trace = new List<string>();

            for (var layer = 0; layer < settings.Layers; layer++)
            {
                trace.Add($"Metadata{layer}");
                trace.Add($"Arrange{layer}");
            }

            trace.Add("Act0");

            for (var layer = settings.Layers - 1; layer >= 0; layer--)
                trace.Add($"Assert{layer}");

            return settings.ExpectedStop == null
                ? trace
                : trace.Take(trace.IndexOf(settings.ExpectedStop) + 1);
        }

        // CL45: Extending a saved composition creates independent sibling paths and preserves every prior value.
        private static void VerifyPersistentExtensions()
        {
            var settings = new Settings();
            var layers = Enumerable.Range(0, 4).Select(layer => CreateLayer(settings, layer)).ToArray();
            var first = new CompositeLab<ProbeModel>(layers[0], layers[1]);
            var left = first.Extend(layers[2]);
            var right = first.Extend(layers[3]);
            var repeated = first.Extend(layers[2]);
            var full = left.Extend(layers[3]);

            VerifyComposition(full, new[] { 0, 1, 2, 3 });
            VerifyComposition(right, new[] { 0, 1, 3 });
            VerifyComposition(left, new[] { 0, 1, 2 });
            VerifyComposition(repeated, new[] { 0, 1, 2 });
            VerifyComposition(first, new[] { 0, 1 });
            VerifyComposition(layers[0], new[] { 0 });
            Assert.That(left, Is.Not.SameAs(repeated));
            Assert.That(layers.Select(lab => lab.Id), Is.EqualTo(new[] { "Layer0", "Layer1", "Layer2", "Layer3" }));
        }

        private static void VerifyComposition(ILab<ProbeModel> composition, int[] layers)
        {
            var model = new ProbeModel();
            var expected = new List<string>();

            foreach (var layer in layers)
            {
                expected.Add($"Metadata{layer}");
                expected.Add($"Arrange{layer}");
            }

            expected.Add("Act0");

            foreach (var layer in layers.Reverse())
                expected.Add($"Assert{layer}");

            composition.Execute(model, out var error);

            Assert.That(error, Is.Null);
            Assert.That(model.Trace, Is.EqualTo(expected));
            Assert.That(
                composition.Id,
                Is.EqualTo(string.Join("__", layers.Reverse().Select(layer => $"Layer{layer}"))));
            Assert.That(model.MetadataGroup.Count, Is.EqualTo(layers.Length));
            Assert.That(model.ExecutionCount, Is.EqualTo(1));
        }
    }
}
