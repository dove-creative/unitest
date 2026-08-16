using System;

namespace UniTest
{
    public class Lab<TModel> : ILab<TModel> where TModel : Model
    {
        // Front
        public string ID { get; set; }

        public const string Extender = "_";
        public const string Separator = "__";
        public const string DefaultID = "Lab";

        public Func<TModel, object> SetMetadata;
        public Action<TModel, SubjectMetadata> Arranger;
        public Action<TModel, SubjectMetadata> Actor;
        public Action<TModel, SubjectMetadata> Asserter;

        // Internal
        public readonly SubjectMetadata MetadataTemplate;


        // Content
        public Lab(string id = DefaultID, Action<TModel, SubjectMetadata> arranger = null, Action<TModel, SubjectMetadata> actor = null, Action<TModel, SubjectMetadata> asserter = null,
            Type expectedExceptionType = null, bool toUnsustainable = false, bool toUncontinuable = false, int remainingExecutionCount = -1)
        {
            ID = id;    
            (Arranger, Actor, Asserter) = (arranger, actor, asserter);

            MetadataTemplate = new()
            {
                ExpectedExceptionType = expectedExceptionType,
                ToUnsustainable = toUnsustainable,
                ToUncontinuable = toUncontinuable,
                RemainingExecutionCount = remainingExecutionCount
            };
        }

        public Lab<TModel> Copy(string id) => new(id, Arranger, Actor, Asserter,
            MetadataTemplate.ExpectedExceptionType, MetadataTemplate.ToUnsustainable, MetadataTemplate.ToUncontinuable, MetadataTemplate.RemainingExecutionCount)
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
            Exception exception = null;

            try
            {
                Actor?.Invoke(model, metadata);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null && (!metadata.ExpectedExceptionType?.IsAssignableFrom(exception.GetType()) ?? true))
                throw exception;

            if (exception == null && metadata.ExpectedExceptionType != null)
                throw new MissingExpectedException($"Expected exception '{metadata.ExpectedExceptionType.Name}' was not thrown.", metadata.ExpectedExceptionType);
        }

        internal void DoAssert(TModel model)
        {
            Asserter?.Invoke(model, model.MetadataGroup[this]);
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


            if (model.MetadataGroup[this].ToUncontinuable)
            {
                SetMetadata();
                model.Continuable = false;

                exception = null;
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
                var meataData = model.MetadataGroup[this];

                if (meataData.ToUncontinuable)
                    model.Continuable = false;
                else if (meataData.ToUnsustainable)
                    model.Sustainable = false;

                if (meataData.RemainingExecutionCount >= 0)
                {
                    if (!model.RemainingExecutionCount.HasValue
                        || model.RemainingExecutionCount.Value > meataData.RemainingExecutionCount)
                    {
                        model.RemainingExecutionCount = meataData.RemainingExecutionCount;
                    }
                }
            }
        }


        public override string ToString() => $"Lab '{ID}'";
    }
}
