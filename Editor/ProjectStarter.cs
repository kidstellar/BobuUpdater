using UnityEditor;
using UnityEngine;
using System.IO;
using System.Net;

public class ProjectStarter : Editor
{
    //Starter
    private static string[] githubUrls = new string[]
    {
        "https://github.com/kidstellar/BobuPackages/raw/main/Dotween.unitypackage", // Buraya GitHub URL'lerini ekleyin.
        "https://github.com/kidstellar/BobuPackages/raw/main/BobuEditor.unitypackage"
    };

    private static bool isImporting = false;

    //Güncelleme
    private const string packageUrl = "https://github.com/kidstellar/BobuPackages/raw/main/BobuEditor.unitypackage";

    //Projeyi çalışma kurulumunu yapar
    [MenuItem("Bobu/Start Project", true)]
    public static bool ValidateStartProject()
    {
        // isImporting true ise menü öğesini devre dışı bırak
        return !isImporting;
    }

    [MenuItem("Bobu/Start Project")]
    public static void StartProject()
    {
        if (isImporting)
        {
            // İşlem tamamlandı, kullanıcıya bilgi ver
            EditorUtility.DisplayDialog("Project Start", "Project start process completed.", "OK");
            Debug.LogWarning("Import process is already in progress.");
            return;
        }

        isImporting = true;

        foreach (var url in githubUrls)
        {
            string fileName = Path.GetFileName(url);
            string downloadPath = Path.Combine("Temp", fileName);
            DownloadAndImportPackage(url, downloadPath);
        }

    }

    private static void DownloadAndImportPackage(string url, string path)
    {
        using (WebClient client = new WebClient())
        {
            try
            {
                // Dosyayı indir
                client.DownloadFile(url, path);
                Debug.Log("Package downloaded successfully: " + path);

                // İndirilen dosyayı import et
                ImportPackage(path);
            }
            catch (WebException ex)
            {
                Debug.LogError("Failed to download the package: " + ex.Message);
            }
        }
    }

    private static void ImportPackage(string path)
    {
        if (File.Exists(path))
        {
            AssetDatabase.ImportPackage(path, false);
            Debug.Log("Package imported successfully: " + path);

            // İndirme ve import işlemi tamamlandıktan sonra geçici dosyayı sil
            File.Delete(path);
        }
        else
        {
            Debug.LogError("Package file not found at path: " + path);
        }
    }

    //Bobu Editörün güncel olup olmadığını kontrol eder
    [MenuItem("Bobu/Update Editor")]
    public static void UpdatePackage()
    {
        DownloadAndInstallPackage();
    }

    private static void DownloadAndInstallPackage()
    {
        string tempDownloadPath = Path.Combine(Application.temporaryCachePath, "BobuEditor.unitypackage");

        using (WebClient client = new WebClient())
        {
            try
            {
                // Paketi indir
                client.DownloadFile(packageUrl, tempDownloadPath);
                Debug.Log("Package downloaded successfully.");

                // Dizinleri sil
                DeleteDirectory(Application.dataPath + "/Assets/Resources/EditorScript");
                DeleteDirectory(Application.dataPath + "/Assets/Resources/Mechanics");

                // Paketi içe aktar
                AssetDatabase.ImportPackage(tempDownloadPath, false);
                Debug.Log("Package imported successfully.");

                // Geçici dosyayı sil
                File.Delete(tempDownloadPath);
            }
            catch (WebException ex)
            {
                Debug.LogError("Failed to download the package: " + ex.Message);
            }
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Debug.Log("Deleted directory: " + path);
        }
        else
        {
            Debug.LogWarning("Directory not found: " + path);
        }
    }
}
