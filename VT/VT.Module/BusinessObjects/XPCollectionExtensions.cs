using DevExpress.Xpo;

namespace VT.Module.BusinessObjects;

public static class XPCollectionExtensions
{
    public static void Clear<T>(this XPCollection<T> collection) where T : XPBaseObject
    {
        throw new NotImplementedException();
        // while (collection.Count > 0)
        // {
        //     collection[0].Delete();
        // }
    }
}
