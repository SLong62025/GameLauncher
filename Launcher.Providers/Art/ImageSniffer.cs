using System;

namespace Launcher.Providers.Art;

//Byte checking just in case something stupid gets fetched
internal static class ImageSniffer {
    public static bool LooksLikeImage(ReadOnlySpan<byte> b) {
        if (b.Length < 12) return false;

        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return true;                    // JPEG
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return true;    // PNG

        return b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'                    // WebP
            && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P';
    }
}
