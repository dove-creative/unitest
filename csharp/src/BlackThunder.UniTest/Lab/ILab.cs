namespace BlackThunder.UniTest
{
    public interface ILab<TModel> where TModel : Model
    {
        string Id { get; }
        void Execute(TModel model, out ExecutionException exception);
    }
}
