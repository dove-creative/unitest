using System;

namespace UniTest
{
    public class CompactLab<TModel> where TModel : Model
    {
        // Front
        public string ID;

        public Action<TModel> Arranger;
        public Action<TModel> Actor;
        public Action<TModel> Asserter;

        public bool ToUnsustainable = false;
        public bool ToUncontinuable = false;
        public int RemainingExecutionCount = -1;


        // Content
        public CompactLab() { }

        public CompactLab(string id, Action<TModel> arranger = null, Action<TModel> actor = null, Action<TModel> asserter = null,
             bool toUnsustainable = false, bool toUncontinuable = false, int remainingExecutionCount = -1)
        {
            ID = id;
            (Arranger, Actor, Asserter) = (arranger, actor, asserter);
            (ToUnsustainable, ToUncontinuable, RemainingExecutionCount) = (toUnsustainable, toUncontinuable, remainingExecutionCount);
        }

        public Lab<TModel> Build() => new(
            id: ID,

            arranger: (m, _) => Arranger?.Invoke(m),
            actor: (m, _) => Actor?.Invoke(m),
            asserter: (m, _) => Asserter?.Invoke(m),

            toUnsustainable: ToUnsustainable,
            toUncontinuable: ToUncontinuable,
            remainingExecutionCount: RemainingExecutionCount);
    }
}
 