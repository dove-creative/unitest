using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackThunder.UniTest
{
    public class CompositeLab<TModel> : ILab<TModel> where TModel : Model
    {
        // Front
        public string Id { get; private set; }

        // Internal
        private List<Lab<TModel>> _labs;


        // Content
        private CompositeLab() { }
        public CompositeLab(Lab<TModel> original, Lab<TModel> extension) : this()
        {
            _labs = new() { original, extension };

            Id = string.Join(Lab<TModel>.Separator, extension.Id, original.Id);
        }

        public CompositeLab<TModel> Extend(Lab<TModel> extension)
        {
            var result = new CompositeLab<TModel>()
            {
                _labs = _labs.ToList(),
            };

            result._labs.Add(extension);
            result.Id = string.Join(Lab<TModel>.Separator, extension.Id, Id);

            return result;
        }

        public void Execute(TModel model, out ExecutionException exception)
        {
            try
            {
                model.DoExecute(Id);
                model.MetadataGroup.Clear();
            }
            catch (Exception ex)
            {
                exception = new("Model setting failed", ex);

                if (model != null)
                    model.Continuable = false;

                return;
            }

            foreach (var lab in _labs)
            {
                try
                {
                    lab.DoArrange(model);
                }
                catch (Exception ex)
                {
                    exception = new($"Arrange failed at '{lab.Id}'", ex);
                    model.Continuable = false;
                    return;
                }
            }

            var actException = _labs[0].CaptureAct(model);
            var expectedTypes = model.MetadataGroup.Values
                .Select(md => md.ExpectedExceptionType)
                .Where(type => type != null)
                .ToList();

            if (actException != null && !expectedTypes.Any(type => type.IsInstanceOfType(actException)))
            {
                exception = new($"Act failed at '{_labs[0].Id}'", actException);
                model.Continuable = false;
                return;
            }

            // Every declared expectation must match, so a layer's type cannot be satisfied by another layer's match.
            var missingType = expectedTypes.FirstOrDefault(type => !type.IsInstanceOfType(actException));

            if (missingType != null)
            {
                exception = new(
                    $"Act failed at '{_labs[0].Id}'",
                    new MissingExpectedException(
                        $"Expected exception '{missingType.Name}' was not thrown.",
                        missingType,
                        actException));
                model.Continuable = false;
                return;
            }


            foreach (var lab in _labs.Reverse<Lab<TModel>>())
            {
                try
                {
                    lab.DoAssert(model);
                }
                catch (Exception ex)
                {
                    exception = new($"Assert failed at '{lab.Id}'", ex);
                    model.Continuable = false;
                    return;
                }
            }

            SetMetadata();
            exception = null;


            void SetMetadata()
            {
                var counts = model.MetadataGroup.Values
                    .Where(md => md.RemainingExecutionCount >= 0)
                    .Select(i => i.RemainingExecutionCount);

                if (counts.Any())
                {
                    var count = counts.Min();

                    if (!model.RemainingExecutionCount.HasValue
                        || model.RemainingExecutionCount.Value > count)
                    {
                        model.RemainingExecutionCount = count;
                    }
                }
                else if (model.MetadataGroup.Values.Any(md => md.ToUnsustainable))
                {
                    model.Sustainable = false;
                }

                if (model.MetadataGroup.Values.Any(md => md.ToUncontinuable))
                    model.Continuable = false;
            }
        }


        public override string ToString() => $"Composite Lab '{Id}'";
    }
}
