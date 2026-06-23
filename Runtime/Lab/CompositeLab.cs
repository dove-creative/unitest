using System;
using System.Linq;
using System.Collections.Generic;

namespace UniTest
{
    public class CompositeLab<TModel> : ILab<TModel> where TModel : Model
    {
        // Front
        public string ID { get; private set; }

        // Internal
        List<Lab<TModel>> labs;


        // Content
        CompositeLab() { }
        public CompositeLab(Lab<TModel> original, Lab<TModel> extension) : this()
        {
            labs = new() { original, extension };
            ID = string.Join(Lab<TModel>.Separator, extension.ID, original.ID);
        }

        public CompositeLab<TModel> Extend(Lab<TModel> extension)
        {
            var result = new CompositeLab<TModel>()
            {
                labs = labs.ToList(),
            };

            result.labs.Add(extension);
            result.ID = string.Join(Lab<TModel>.Separator, extension.ID, ID);

            return result;
        }

        public void Execute(TModel model, out ExecutionException exception)
        {
            try
            {
                model.DoExecute(ID);
                model.MetadataGroup.Clear();
            }
            catch (Exception ex)
            {
                exception = new("Model setting failed", ex);

                if (model != null)
                    model.Continuable = false;

                return;
            }

            foreach (var lab in labs)
            {
                try
                {
                    lab.DoArrange(model);
                }
                catch (Exception ex)
                {
                    exception = new($"Arrange failed at '{lab.ID}'", ex);
                    model.Continuable = false;
                    return;
                }
            }

            bool expectedExceptionThrown = false;

            try
            {
                labs[0].DoAct(model);
            }
            catch (Exception ex)
            {
                if (!model.MetadataGroup.Values.Any(md => md.ExpectedExceptionType?.IsAssignableFrom(ex.GetType()) ?? false))
                {
                    exception = new($"Act failed at '{labs[0].ID}'", ex);
                    model.Continuable = false;
                    return;
                }

                expectedExceptionThrown = true;
            }

            if (!expectedExceptionThrown)
            {
                var missingExpectedException = model.MetadataGroup
                    .Where(pair => !ReferenceEquals(pair.Key, labs[0]))
                    .Select(pair => pair.Value.ExpectedExceptionType)
                    .FirstOrDefault(type => type != null);

                if (missingExpectedException != null)
                {
                    exception = new($"Act failed at '{labs[0].ID}'",
                        new MissingExpectedException(
                            $"Expected exception '{missingExpectedException.Name}' was not thrown.",
                            missingExpectedException));
                    model.Continuable = false;
                    return;
                }
            }


            if (model.MetadataGroup.Values.Any(md => md.ToUncontinuable))
            {
                SetMetadata();
                model.Continuable = false;

                exception = null;
                return;
            }


            foreach (var lab in labs.Reverse<Lab<TModel>>())
            {
                try
                {
                    lab.DoAssert(model);
                }
                catch (Exception ex)
                {
                    exception = new($"Assert failed at '{lab.ID}'", ex);
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
            }
        }


        public override string ToString() => $"Composite Lab '{ID}'";
    }
}
