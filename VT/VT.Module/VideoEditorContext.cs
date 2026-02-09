using DevExpress.ExpressApp;
using VT.Module.BusinessObjects;



namespace VT.Module;

public class VideoEditorContext
{
    public IObjectSpace ObjectSpace { get; set; }
    public VideoProject CurrentVideoProject { get; set; }
}