namespace BobuEditor
{
    using System.IO;
    using System.Net;
    using UnityEditor;
    using UnityEngine;

    public class ImageEditor : Editor
    {
        //Ýndirme url yolu
        private const string ImageBundleUrl = "https://github.com/kidstellar/BobuPackages/raw/main/ImageBundleEditor.unitypackage";

        //Menü de gözüken alan
        [MenuItem("Bobu/Updater/Other Editors/Image Bundle Builder")]
        public static void UpdatePackageImage()
        {
            DownloadAndInstallPackageImage();
        }

        // Yükleme bölümü
        private static void DownloadAndInstallPackageImage()
        {
            string tempDownloadPath = Path.Combine(Application.temporaryCachePath, "ImageBundleEditor.unitypackage");

            using (WebClient client = new WebClient())
            {
                try
                {
                    // Paketi indir
                    client.DownloadFile(ImageBundleUrl, tempDownloadPath);
                    Debug.Log("Package downloaded successfully.");

                    // Dizinleri sil
                    DeleteDirectory("Resources/ImageBundleBuilder");

                    // Paketi içe aktar
                    AssetDatabase.ImportPackage(tempDownloadPath, false);
                    Debug.Log("Package imported successfully.");

                    // Geçici dosyayý sil
                    File.Delete(tempDownloadPath);
                }
                catch (WebException ex)
                {
                    Debug.LogError("Failed to download the package: " + ex.Message);
                }
            }
        }

        //indirilen dosyalarý ve yollarý temizlemek için kullanýlýyor
        private static void DeleteDirectory(string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, relativePath);

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                FileUtil.DeleteFileOrDirectory(relativePath); // Unity'nin kendi API'sini kullanarak silme iþlemi
                FileUtil.DeleteFileOrDirectory(relativePath + ".meta"); // Meta dosyasýný da silme
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