using UnityEngine.Networking;

public class BypassCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // ⚠ xavfli: har qanday certni qabul qiladi
    }
}
