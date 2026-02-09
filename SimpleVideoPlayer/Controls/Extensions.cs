public static class Extensions
{
    public static int ConvertToControlVolumeValue(this float volumeValue)
    {
        if (volumeValue < 0)
            return 0;
        if (volumeValue > 1)
            return 100;
        return (int)(volumeValue * 100);
    }
}