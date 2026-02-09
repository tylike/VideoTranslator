using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Serilog;
using System.Diagnostics;
using System.Windows;
using TrackMenuAttributes;
using VideoTranslator.Services;

namespace VT.Module.BusinessObjects;

public class SRTSource(Session s) : MediaSource(s)
{
    private readonly ILogger _logger = Log.ForContext<SRTSource>();    
}

