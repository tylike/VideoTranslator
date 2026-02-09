using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DevExpress.XtraRichEdit.Import.OpenXml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoTranslator.Interfaces;

namespace VT.Module.BusinessObjects;
[NonPersistent]
public abstract class VTBaseObject(Session s) : XPObject(s),IServices
{
	[XafDisplayName("创建时间")]
	[ModelDefault("DisplayFormat","yyyy-MM-dd HH:mm:ss")]
	public DateTime CreateTime
	{
		get { return GetPropertyValue<DateTime>(nameof(CreateTime)); }
		set { SetPropertyValue(nameof(CreateTime), value); }
	}

    [XafDisplayName("更新时间")]
    [ModelDefault("DisplayFormat", "yyyy-MM-dd HH:mm:ss")]
    public DateTime LastUpdateTime
	{
		get { return GetPropertyValue<DateTime>(nameof(LastUpdateTime)); }
		set { SetPropertyValue(nameof(LastUpdateTime), value); }
	}

    public IServiceProvider ServiceProvider { get =>Session.ServiceProvider; set => throw new NotImplementedException(); }

    public override void AfterConstruction()
    {
        base.AfterConstruction();
		this.CreateTime = DateTime.Now;
    }

    public VideoProject GetCurrentVideoProject()
    {
        throw new NotImplementedException();
    }

    protected override void OnSaving()
    {
        base.OnSaving();
		this.LastUpdateTime = DateTime.Now;
    }
}
