using System.Collections.Generic;
using UnityEditor;

//빌드 클래스
public static class BuildAutomator
{
    public static void Build()
    {
        // 빌드 세팅 씬 가져오기
        List<string> scenes = new List<string>();

        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        // 빌드 설정
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes.ToArray();
        options.locationPathName = $"C:\\Build\\{PlayerSettings.productName}.exe";
        options.target = BuildTarget.StandaloneWindows;
        options.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(options);
    }
}