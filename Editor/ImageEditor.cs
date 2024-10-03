namespace BobuEditor
{
    using System.IO;
    using System.Net;
    using UnityEditor;
    using UnityEngine;

    public class ImageEditor : Editor
    {
        //�ndirme url yolu
        private const string ImageBundleUrl = "https://github.com/kidstellar/BobuPackages/raw/main/ImageBundleEditor.unitypackage";

        //Men� de g�z�ken alan
        [MenuItem("Bobu/Updater/Other Editors/Image Bundle Builder")]
        public static void UpdatePackageImage()
        {
            DownloadAndInstallPackageImage();
        }

        // Y�kleme b�l�m�
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

                    // Paketi i�e aktar
                    AssetDatabase.ImportPackage(tempDownloadPath, false);
                    Debug.Log("Package imported successfully.");

                    // Ge�ici dosyay� sil
                    File.Delete(tempDownloadPath);
                }
                catch (WebException ex)
                {
                    Debug.LogError("Failed to download the package: " + ex.Message);
                }
            }
        }

        //indirilen dosyalar� ve yollar� temizlemek i�in kullan�l�yor
        private static void DeleteDirectory(string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, relativePath);

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                FileUtil.DeleteFileOrDirectory(relativePath); // Unity'nin kendi API'sini kullanarak silme i�lemi
                FileUtil.DeleteFileOrDirectory(relativePath + ".meta"); // Meta dosyas�n� da silme
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