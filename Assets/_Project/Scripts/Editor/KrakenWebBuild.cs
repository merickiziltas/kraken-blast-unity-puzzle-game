using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace KrakenCache.Editor
{
    public static class KrakenWebBuild
    {
        [MenuItem("Kraken Cache/Build Web Demo")]
        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new BuildFailedException("Install Web Build Support for this Unity version, then try again.");

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            var output = Path.GetFullPath("WebBuild");
            var oldCompression = PlayerSettings.WebGL.compressionFormat;
            var oldFallback = PlayerSettings.WebGL.decompressionFallback;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.WebGL
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException($"Web build ended with {report.summary.result}.");

                File.WriteAllText(Path.Combine(output, ".nojekyll"), string.Empty);
                string index = Path.Combine(output, "index.html");
                string html = File.ReadAllText(index);
                File.WriteAllText(index, html.Replace("</head>", PortraitStyle + "\n</head>"));
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = oldCompression;
                PlayerSettings.WebGL.decompressionFallback = oldFallback;
            }
        }

        // Retain Unity's generated loader and file names.
        private const string PortraitStyle = @"<style>
          html,body{margin:0;background:#0b1322;color:#f7e6c6}
          #unity-container.unity-desktop,#unity-container.unity-mobile{
            position:fixed;inset:0;transform:none;display:grid;place-items:center;
            width:100%;height:100dvh;background:#0b1322}
          #unity-canvas{width:min(100vw,56.25dvh)!important;height:min(177.7778vw,100dvh)!important;
            display:block;background:#101c2e;touch-action:none}
          #unity-footer{display:none} #unity-warning{max-width:80vw}
        </style>";
    }
}
