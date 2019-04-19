namespace NServiceBus
{
    using System;
    using System.Diagnostics;
#if NETSTANDARD
    using System.Runtime.InteropServices;
#endif
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Particular.Licensing;

    class LicenseManager
    {
        internal bool HasLicenseExpired => false;

        internal void InitializeLicense(string licenseText, string licenseFilePath)
        {
        }
    }
}
