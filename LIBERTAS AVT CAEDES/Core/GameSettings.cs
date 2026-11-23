using Raylib_cs;
using LIBERTAS_AVT_CAEDES.Telemetry;

namespace LIBERTAS_AVT_CAEDES.Core
{
    public sealed class GameSettings
    {
        public const string BloomFragShader = @"
#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;

uniform sampler2D texture0;
uniform vec2 resolution;

void main()
{
    vec2 texel = 1.0 / resolution;
    vec4 baseColor = texture(texture0, fragTexCoord);
    vec3 bloomSum = vec3(0.0);

    // 9-tap Gaussian-weighted bright-pass bloom kernel
    for (int x = -2; x <= 2; x++)
    {
        for (int y = -2; y <= 2; y++)
        {
             vec2 offset = vec2(float(x), float(y)) * texel * 2.5;
             vec3 sampleCol = texture(texture0, fragTexCoord + offset).rgb;
             float brightness = dot(sampleCol, vec3(0.2126, 0.7152, 0.0722));
             if (brightness > 0.58)
             {
                 bloomSum += sampleCol * (brightness - 0.58);
             }
        }
    }

    bloomSum /= 14.0;
    finalColor = vec4(baseColor.rgb + bloomSum * 1.35, baseColor.a) * fragColor;
}";

        public bool AntiAliasingEnabled { get; private set; } = true;
        public bool BloomEnabled { get; private set; } = true;

        public bool TelemetryConsent
        {
            get => PlayerTelemetry.ConsentGranted;
            set => PlayerTelemetry.ConsentGranted = value;
        }

        public void ToggleAA()
        {
            AntiAliasingEnabled = !AntiAliasingEnabled;
            if (AntiAliasingEnabled)
                Raylib.SetWindowState(ConfigFlags.Msaa4xHint);
            else
                Raylib.ClearWindowState(ConfigFlags.Msaa4xHint);
        }

        public void ToggleBloom() => BloomEnabled = !BloomEnabled;

        public void ToggleTelemetryConsent() => TelemetryConsent = !TelemetryConsent;
    }
}
