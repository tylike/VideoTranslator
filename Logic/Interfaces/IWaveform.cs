using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace VideoTranslator.Interfaces;

public interface IWaveform : INotifyPropertyChanged
{
    #region 波形数据属性

    List<double> WaveformData { get; set; }

    bool ShowWaveform { get; set; }

    int WaveformSamplesPerSecond { get; }

    #endregion
}
