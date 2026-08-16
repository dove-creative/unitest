using System.Collections.Generic;

namespace UniTest
{
    public static partial class Tools
    {
        public static ILab<TModel> Extend<TModel>(this Lab<TModel> extension, ILab<TModel> original) where TModel : Model
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
        public static IEnumerable<ILab<TModel>> Extend<TModel>(this Lab<TModel> extension, IEnumerable<ILab<TModel>> originals) where TModel : Model
        {
            foreach (var original in originals)
                yield return Extend(extension, original);
        }


        public static Lab<TModel> Merge<TModel>(this Lab<TModel> lab, Lab<TModel> template,
            bool useSetMetadata = true, bool useArranger = true, bool useActor = true, bool useAsserter = true)
            where TModel : Model
        {
            var id = template.ID != Lab<TModel>.DefaultID
                ? string.Join(Lab<TModel>.Extender, lab.ID, template.ID)
                : lab.ID;

            var _lab = lab.Copy(id);

            _lab.SetMetadata = (useSetMetadata ? template.SetMetadata : null) + _lab.SetMetadata;
            _lab.Arranger = (useArranger ? template.Arranger : null) + _lab.Arranger;
            _lab.Actor = (useActor ? template.Actor : null) + _lab.Actor;
            _lab.Asserter = (useAsserter ? template.Asserter : null) + _lab.Asserter;

            _lab.MetadataTemplate.Merge(template.MetadataTemplate);

            return _lab; 
        }
        public static IEnumerable<Lab<TModel>> Merge<TModel>(this Lab<TModel> lab, params Lab<TModel>[] templates) where TModel : Model
        {
            return Merge(lab, (IEnumerable<Lab<TModel>>)templates);
        }
        public static IEnumerable<Lab<TModel>> Merge<TModel>(this Lab<TModel> lab, IEnumerable<Lab<TModel>> templates,
            bool useSetMetadata = true, bool useArranger = true, bool useActor = true, bool useAsserter = true)
            where TModel : Model
        {
            foreach (var template in templates)
                yield return Merge(lab, template, useSetMetadata, useArranger, useActor, useAsserter);
        }
    }
}
