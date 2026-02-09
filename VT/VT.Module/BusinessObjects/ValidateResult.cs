using DevExpress.ExpressApp;
using System;
using System.Linq;

namespace VT.Module.BusinessObjects
{
    public class ValidateResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public void Validate()
        {
            if (!Success)
                throw new UserFriendlyException(ErrorMessage);
        }
    }
}