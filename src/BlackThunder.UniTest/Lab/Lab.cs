using System;
using System.Runtime.ExceptionServices;

namespace BlackThunder.UniTest
{
    public class Lab<TModel> : ILab<TModel> where TModel : Model
    {
        // Front
        public string Id { get; init; }

        public const string Extender = "_";
        public const string Separator = "__";
        public const string DefaultId = "Lab";

        public Func<TModel, object> SetMetadata { get; init; }
        public Action<TModel, SubjectMetadata> Arranger { get; init; }
        public Action<TModel, SubjectMetadata> Actor { get; init; }
        public Action<TModel, SubjectMetadata> Asserter { get; init; }

        public IReadOnlySubjectMetadata MetadataTemplate { get; }


        // Content
        public Lab(
            string id = DefaultId,
            Action<TModel, SubjectMetadata> arranger = null,
            Action<TModel, SubjectMetadata> actor = null,
            Action<TModel, SubjectMetadata> asserter = null,
            Type expectedExceptionType = null,
            bool toUnsustainable = false,
            bool toUncontinuable = false,
            int remainingExecutionCount = -1)
        {
            Id = id;
            (Arranger, Actor, Asserter) = (arranger, actor, asserter);

            MetadataTemplate = new SubjectMetadata
            {
                ExpectedExceptionType = expectedExceptionType,
                ToUnsustainable = toUnsustainable,
                ToUncontinuable = toUncontinuable,
                RemainingExecutionCount = remainingExecutionCount
            };
        }

        public Lab<TModel> Copy(string id) => new(
            id,
            Arranger,
            Actor,
            Asserter,
            MetadataTemplate.ExpectedExceptionType,
            MetadataTemplate.ToUnsustainable,
            MetadataTemplate.ToUncontinuable,
            MetadataTemplate.RemainingExecutionCount)
        { SetMetadata = SetMetadata };


        internal void DoArrange(TModel model)
        {
            var metadata = MetadataTemplate.Copy();
            metadata.Metadata = SetMetadata?.Invoke(model);

            model.MetadataGroup[this] = metadata;
            Arranger?.Invoke(model, metadata);
        }

        internal void DoAct(TModel model)
        {
            var metadata = model.MetadataGroup[this];
            var exception = CaptureAct(model);

            if (exception != null && (!metadata.ExpectedExceptionType?.IsAssignableFrom(exception.GetType()) ?? true))
                ExceptionDispatchInfo.Capture(exception).Throw();

            if (exception == null && metadata.ExpectedExceptionType != null)
                throw new MissingExpectedException(
                    $"Expected exception '{metadata.ExpectedExceptionType.Name}' was not thrown.",
                    metadata.ExpectedExceptionType);
        }

        // Returns the Actor's exception unjudged, because a composition compares it with every layer's expectation.
        internal Exception CaptureAct(TModel model)
        {
            try
            {
                Actor?.Invoke(model, model.MetadataGroup[this]);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        internal void DoAssert(TModel model) => Asserter?.Invoke(model, model.MetadataGroup[this]);


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

            try
            {
                DoArrange(model);
            }
            catch (Exception ex)
            {
                exception = new("Arrange failed", ex);
                model.Continuable = false;

                return;
            }

            try
            {
                DoAct(model);
            }
            catch (Exception ex)
            {
                exception = new("Act failed", ex);
                model.Continuable = false;

                return;
            }


            try
            {
                DoAssert(model);
            }
            catch (Exception ex)
            {
                exception = new("Assert failed", ex);
                model.Continuable = false;
                return;
            }


            SetMetadata();
            exception = null;


            void SetMetadata()
            {
                var metadata = model.MetadataGroup[this];

                if (metadata.ToUncontinuable)
                    model.Continuable = false;
                else if (metadata.ToUnsustainable)
                    model.Sustainable = false;

                if (metadata.RemainingExecutionCount >= 0)
                {
                    if (!model.RemainingExecutionCount.HasValue
                        || model.RemainingExecutionCount.Value > metadata.RemainingExecutionCount)
                    {
                        model.RemainingExecutionCount = metadata.RemainingExecutionCount;
                    }
                }
            }
        }


        public override string ToString() => $"Lab '{Id}'";
    }
}
