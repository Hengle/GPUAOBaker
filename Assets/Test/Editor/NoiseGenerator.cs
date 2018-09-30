using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NoiseGenerator {

    [MenuItem("Test/GenerateNoise")]
    static void Generate()
    {
        string savePath = EditorUtility.SaveFilePanel("", "", "", "png");
        if (string.IsNullOrEmpty(savePath))
            return;

        Texture2D tex = new Texture2D(256, 256);

        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 256; j++)
            {
                float rad1 = Random.Range(0.0f, 1.0f);
                float rad2 = Random.Range(0.0f, 1.0f);
                tex.SetPixel(i, j, new Color(rad1, rad2, 0, 1.0f));
            }
        }
        tex.Apply();

        byte[] buffer = tex.EncodeToPNG();

        System.IO.File.WriteAllBytes(savePath, buffer);

        Object.DestroyImmediate(tex);
    }
}
