using System;
using System.Linq;

namespace VT.Module.BusinessObjects
{
    public class ValidateFileResult : ValidateResult
    {
        public ValidateFileResult(string fileFullName, string fileMemo)
        {
            if (string.IsNullOrEmpty(fileFullName))
            {
                Success = false;
                ErrorMessage = $"还没有{fileMemo}";
                return;
            }
            if (!File.Exists(fileFullName))
            {
                Success = false;
                ErrorMessage = $"{fileMemo}不存在,路径{fileFullName}";
                return;
            }
            Success = true;
        }
    }
}
