
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class OllamaJsonValidationTests
{
    private bool TestJson(string json)
    {
        Type serviceType = null;

        foreach (Type candidate in
            UnityEditor.TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
        {
            if (candidate.Name == "OllamaService")
            {
                serviceType = candidate;
                break;
            }
        }

        Assert.IsNotNull(serviceType, "OllamaService bulunamadý.");

        GameObject testObject = new GameObject("LLM_Test");

        try
        {
            Component service = testObject.AddComponent(serviceType);

            MethodInfo parseMethod = serviceType.GetMethod(
                "TryParseRiddleResult",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(parseMethod, "JSON doðrulama metodu bulunamadý.");

            object[] parameters = { json, null };

            return (bool)parseMethod.Invoke(service, parameters);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }
    }

    [Test]
    public void BozukJsonReddedilir()
    {
        string json = "{bozuk JSON !!!}";

        bool result = TestJson(json);

        Assert.IsFalse(result);
    }

    [Test]
    public void BosYanitReddedilir()
    {
        string json =
            "{\"is_correct\":false,\"owl_response\":\"\"}";

        bool result = TestJson(json);

        Assert.IsFalse(result);
    }

    [Test]
    public void GecerliJsonKabulEdilir()
    {
        string json =
            "{\"is_correct\":true,\"owl_response\":\"Dogru cevap!\"}";

        bool result = TestJson(json);

        Assert.IsTrue(result);
    }
    [Test]
    public void IsCorrectAlaniEksikseReddedilir()
    {
        string json = "{\"owl_response\":\"Merhaba\"}";

        bool result = TestJson(json);

        Assert.IsFalse(result);
    }

    [Test]
    public void OwlResponseAlaniEksikseReddedilir()
    {
        string json = "{\"is_correct\":false}";

        bool result = TestJson(json);

        Assert.IsFalse(result);
    }
}
