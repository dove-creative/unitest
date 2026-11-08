using System.Collections.Generic;

namespace BlackThunder.UniTest
{
    public static partial class Tools
    {
        public static ILab<TModel> Extend<TModel>(
            this Lab<TModel> extension,
            ILab<TModel> original) where TModel : Model
        {
            if (original is Lab<TModel> _lab)
            {
                return new CompositeLab<TModel>(_lab, extension);
            }
            else if (original is CompositeLab<TModel> _cLab)
            {
                return _cLab.Extend(extension);
            }
            else
                throw new System.NotSupportedException($"Cannot extend lab of unknown type '{original.GetType()}'");
        }
        public static IEnumerable<ILab<TModel>> Extend<TModel>(
            this Lab<TModel> extension,
            IEnumerable<ILab<TModel>> originals) where TModel : Model
        {
            foreach (var original in originals)
                yield return Extend(extension, original);
        }


        public static Lab<TModel> Merge<TModel>(
            this Lab<TModel> lab,
            Lab<TModel> template,
            bool useSetMetadata = true,
            bool useArranger = true,
            bool useActor = true,
            bool useAsserter = true)
            where TModel : Model
        {
            var id = template.Id != Lab<TModel>.DefaultId
                ? string.Join(Lab<TModel>.Extender, lab.Id, template.Id)
                : lab.Id;

            var metadata = lab.MetadataTemplate.Copy();
            metadata.Merge(template.MetadataTemplate);

            return new Lab<TModel>(
                id,
                (useArranger ? template.Arranger : null) + lab.Arranger,
                (useActor ? template.Actor : null) + lab.Actor,
                (useAsserter ? template.Asserter : null) + lab.Asserter,
                metadata.ExpectedExceptionType,
                metadata.ToUnsustainable,
                metadata.ToUncontinuable,
                metadata.RemainingExecutionCount)
            {
                SetMetadata = (useSetMetadata ? template.SetMetadata : null) + lab.SetMetadata
            };
        }
        public static IEnumerable<Lab<TModel>> Merge<TModel>(
            this Lab<TModel> lab,
            params Lab<TModel>[] templates) where TModel : Model =>
            Merge(lab, (IEnumerable<Lab<TModel>>)templates);
        public static IEnumerable<Lab<TModel>> Merge<TModel>(
            this Lab<TModel> lab,
            IEnumerable<Lab<TModel>> templates,
            bool useSetMetadata = true,
            bool useArranger = true,
            bool useActor = true,
            bool useAsserter = true)
            where TModel : Model
        {
            foreach (var template in templates)
                yield return Merge(lab, template, useSetMetadata, useArranger, useActor, useAsserter);
        }
    }
}
