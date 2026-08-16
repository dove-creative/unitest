namespace UniTest
{
    public interface ILab<TModel> where TModel : Model
    {
        string ID { get; }
        void Execute(TModel model, out ExecutionException exception);
    }
}
