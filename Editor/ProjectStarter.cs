namespace BobuEditor
{
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using System.Net;

    public class ProjectStarter : Editor
    {
        //Start Porject butonuna tıklandığında listede bulunan dosyaların kurulumu yapılır
        //ÖNEMLİ NOT: Sakın listenin 0 indexindeki BobuEditor.unitypackage dosyasının yerini değiştirmeyin - Nedeni: 118.Satır bakınız
        private static string[] githubUrls = new string[]
        {
        "https://github.com/kidstellar/BobuPackages/raw/main/BobuEditor.unitypackage",
        "https://github.com/kidstellar/BobuPackages/raw/main/Dotween.unitypackage"
        };

        //Dosyaların varlığını kontrol ederek Start Project ve Update Editor butonları açık mı kapalı mı olacağı belirlenir
        private static bool isImporting = false;

        // Projeyi çalışma kurulumunu yapar
        [MenuItem("Bobu/Updater/Start Project", true)]
        public static bool ValidateStartProject()
        {
            // Gerekli klasörler var mı kontrol et
            isImporting = Directory.Exists(Path.Combine(Application.dataPath, "Resources/EditorScript")) ||
                                Directory.Exists(Path.Combine(Application.dataPath, "Resources/Mechanics")) ||
                                Directory.Exists(Path.Combine(Application.dataPath, "Plugins"));

            // isImporting true ise veya klasörler varsa menü öğesini devre dışı bırak
            return !isImporting;
        }

        //Projeyi Hazırlar
        [MenuItem("Bobu/Updater/Start Project")]
        public static void StartProject()
        {
            if (isImporting)
            {
                // İşlem tamamlandı, kullanıcıya bilgi ver
                EditorUtility.DisplayDialog("Project Start", "Project start process completed.", "OK");
                Debug.LogWarning("Import process is already in progress.");
                return;
            }

            foreach (var url in githubUrls)
            {
                string fileName = Path.GetFileName(url);
                string downloadPath = Path.Combine("Temp", fileName);
                DownloadAndImportPackage(url, downloadPath);
            }

        }

        //İndirme Methodu
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

        //İndirmeden sonra .unitypacket dosyasını dışarı dışarı çıkartır
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

        // Bobu Editörün güncel olup olmadığını kontrol eder
        [MenuItem("Bobu/Updater/Update Editor", true)]
        public static bool ValidateUpdatePackage()
        {
            // Gerekli klasörler var mı kontrol et
            bool foldersExist = Directory.Exists(Path.Combine(Application.dataPath, "Resources/EditorScript")) ||
                                Directory.Exists(Path.Combine(Application.dataPath, "Resources/Mechanics")) ||
                                Directory.Exists(Path.Combine(Application.dataPath, "Plugins"));

            // Klasörler varsa menü öğesini etkinleştir
            return foldersExist;
        }

        // Editörün güncellenmesini sağlar
        [MenuItem("Bobu/Updater/Update Editor")]
        public static void UpdatePackage()
        {
            DownloadAndInstallPackage();
        }

        // Güncelleme bölümü
        private static void DownloadAndInstallPackage()
        {
            string tempDownloadPath = Path.Combine(Application.temporaryCachePath, "BobuEditor.unitypackage");

            using (WebClient client = new WebClient())
            {
                try
                {
                    // Paketi indir
                    client.DownloadFile(githubUrls[0], tempDownloadPath);
                    Debug.Log("Package downloaded successfully.");

                    // Dizinleri sil
                    DeleteDirectory("Resources/EditorScript");
                    DeleteDirectory("Resources/Mechanics");

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

        //indirilen dosyaları ve yolları temizlemek için kullanılıyor
        private static void DeleteDirectory(string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, relativePath);

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                FileUtil.DeleteFileOrDirectory(relativePath); // Unity'nin kendi API'sini kullanarak silme işlemi
                FileUtil.DeleteFileOrDirectory(relativePath + ".meta"); // Meta dosyasını da silme
                AssetDatabase.Refresh();
                Debug.Log("Deleted directory: " + relativePath);
            }
            else
            {
                Debug.LogWarning("Directory not found: " + relativePath);
            }
        }
    }

}