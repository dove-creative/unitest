using System.Collections.Generic;
using System.Linq;

namespace UniTest
{
    public abstract class CompactProject<TModel> : Project<TModel> where TModel : Model, new()
    {
        public sealed override IEnumerable<ILab<TModel>> CreateLabs(TModel model)
        {
            return CreateCompactLabs(model).Select(l => l.Build());
        }

        public abstract IEnumerable<CompactLab<TModel>> CreateCompactLabs(TModel model);
    }
}
