namespace BobuEditor
{
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using UnityEngine.Networking;
    using System.Threading.Tasks;
    using System.IO;

    public class SoundLibraryWindow : EditorWindow
    {
        string packagePath = "Packages/com.kidstellar.bobuupdater/Editor/SoundsDataOBJ"; // Paket içindeki Sounds klasörünün yolu

        private string searchQuery = ""; // Ses arama kutucuðu deðiþkeni
        private SoundTag tagFilter = SoundTag.All;  // Tür ile arama
        private SubSoundTag subTagFilter = SubSoundTag.None;  // Alt tür için filtre
        private SoundObject[] allSounds;
        private Vector2 scrollPos;

        private bool[] isDownloading; // Hangi seslerin indirildiðini izler
        private bool[] isDownloaded; // Hangi seslerin indirildiðini izler
        private string[] fileSizes; // Dosya boyutlarýný saklar
        private int currentPlayingIndex = -1; // Þu anda çalan sesin indeksini tutar (-1: Hiçbiri çalmýyor)
        private bool[] isPlaying; // Her sesin çalma durumunu izler

        private UnityEditor.EditorApplication.CallbackFunction destructionAction = null;

        [MenuItem("Bobu/Sound Library")]
        public static void ShowWindow()
        {
            var window = GetWindow<SoundLibraryWindow>("Sound Library");

            // Pencerenin minimum ve maksimum boyutunu ayarla
            window.minSize = new Vector2(560, 600); // Minimum boyut
            window.maxSize = new Vector2(560, 600); // Maksimum boyut
        }

        private async void OnEnable()
        {
            // SoundObject varlýklarýný paket içinde arama
            string[] guids = AssetDatabase.FindAssets("t:SoundObject", new[] { packagePath });

            if (guids.Length > 0)
            {
                // Paket içinde ses dosyalarý bulundu, bunlarý yüklüyoruz
                allSounds = new SoundObject[guids.Length];

                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    allSounds[i] = AssetDatabase.LoadAssetAtPath<SoundObject>(assetPath);
                }
            }
            else
            {
                // Paket içerisinde ses bulunamadý, Resources klasörünü kullan
                allSounds = Resources.FindObjectsOfTypeAll<SoundObject>();
            }

            isDownloading = new bool[allSounds.Length]; // Ýndirme iþlemi süren sesler
            isDownloaded = new bool[allSounds.Length];  // Ýndirilen sesler

            fileSizes = new string[allSounds.Length];
            isPlaying = new bool[allSounds.Length]; // Tüm seslerin çalma durumu baþlangýçta false olacak (yani çalmýyor)

            for (int i = 0; i < allSounds.Length; i++)
            {
                // Dosya boyutunu al ve fileSizes dizisine kaydet
                fileSizes[i] = await GetFileSizeFromURL(allSounds[i].downloadLink);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Ses Kütüphanesi", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            // Arama Barý
            GUILayout.Label("Arama:", GUILayout.Width(50));
            searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.Width(200));

            // Tür filtresi
            GUILayout.Label("Tür:", GUILayout.Width(30));
            tagFilter = (SoundTag)EditorGUILayout.EnumPopup(tagFilter, GUILayout.Width(100));

            // Alt tür filtresi
            GUILayout.Label("Alt Tür:", GUILayout.Width(50));
            subTagFilter = (SubSoundTag)EditorGUILayout.EnumPopup(subTagFilter, GUILayout.Width(100));

            GUILayout.EndHorizontal();

            // Dýþ alan stili
            GUIStyle outerBoxStyle = new GUIStyle(GUI.skin.box);
            outerBoxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 1f));  // Koyu dýþ alan rengi

            // ScrollView için dýþ alan
            GUILayout.BeginVertical(outerBoxStyle);

            // Scrollable alan
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            // Filtrelenmiþ sesleri listele
            var filteredSounds = allSounds
                .Where(sound => string.IsNullOrEmpty(searchQuery) || sound.soundName.ToLower().Contains(searchQuery.ToLower()))
                .Where(sound => tagFilter == SoundTag.All || sound.tag == tagFilter)  // All seçeneðinde filtreyi atla
                .Where(sound => subTagFilter == SubSoundTag.None || sound.subTag == subTagFilter)  // Alt tür filtreleme
                .ToList();

            for (int i = 0; i < filteredSounds.Count; i++)
            {
                var sound = filteredSounds[i];

                // Ýndekse göre farklý renkler
                Color bgColor = i % 2 == 0 ? new Color(0.2f, 0.2f, 0.2f, 1f) : new Color(0.15f, 0.15f, 0.15f, 1f);
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = MakeTex(2, 2, bgColor);

                GUILayout.BeginHorizontal(boxStyle);

                // Ses adý için sabit geniþlik ayarý
                GUILayout.Label(sound.soundName, GUILayout.Width(275));

                // Eðer indirme iþlemi sürüyorsa indirme simgesi göster
                if (isDownloading[i])
                {
                    GUILayout.Label(EditorGUIUtility.IconContent("d_Progress"), GUILayout.Width(20), GUILayout.Height(20));
                }
                // Eðer indirme tamamlandýysa tik simgesi göster
                else if (isDownloaded[i])
                {
                    GUILayout.Label(EditorGUIUtility.IconContent("d_FilterSelectedOnly"), GUILayout.Width(20), GUILayout.Height(20));
                }
                else
                {
                    GUILayout.Space(25); // Simge için boþluk
                }

                // Dosya boyutunu göster (örneðin 4.5 MB, 300 KB)
                GUILayout.Label(fileSizes[i], GUILayout.Width(60));

                // Dinle/Durdur butonu
                string buttonLabel = isPlaying[i] ? "Durdur" : "Dinle";
                if (GUILayout.Button(buttonLabel, GUILayout.Width(80)))
                {
                    PlayClipFromURL(sound.downloadLink, i); // Sesin URL'sini ve indeksini vererek çalma/durdurma iþlemini yap
                }

                // Ýndirme butonu için sabit geniþlik ayarý
                if (GUILayout.Button("Ýndir", GUILayout.Width(80)))
                {
                    isDownloading[i] = true;
                    DownloadAndSaveClip(sound.downloadLink, sound.soundName, i);  // Ýndirme durumu ve index ile
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();  // Dýþ alaný kapat
        }

        private async void DownloadAndSaveClip(string url, string fileName, int index)
        {
            string folderPath = Path.Combine(Application.dataPath, "Sounds");

            // Sounds klasörünü oluþtur (eðer yoksa)
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileExtension = Path.GetExtension(url);
            string fullPath = Path.Combine(folderPath, fileName + fileExtension);

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error downloading file: {www.error}. URL: {url}");
                }
                else
                {
                    // Ýndirilen veriyi byte dizisi olarak al
                    byte[] fileData = www.downloadHandler.data;

                    // Dosyayý belirtilen yola kaydet
                    File.WriteAllBytes(fullPath, fileData);
                    Debug.Log($"Dosya kaydedildi: {fullPath}");

                    // Dosya sistemini yenile (Unity'nin dosyalarý görmesi için)
                    AssetDatabase.Refresh();

                    // Ýndirme tamamlandý, indirme simgesini kapat ve tik iþaretini göster
                    isDownloading[index] = false;
                    isDownloaded[index] = true;

                    // Tik iþaretini belirli bir süre göster, ardýndan kaybolsun
                    await Task.Delay(2000); // 2 saniye bekle
                    isDownloaded[index] = false;
                }
            }
        }

        // Texture oluþturmak için bir yardýmcý fonksiyon
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        private async void PlayClipFromURL(string url, int index)
        {
            // Eðer baþka bir ses çalýyorsa, önce onu durdur
            if (currentPlayingIndex != -1 && currentPlayingIndex != index)
            {
                GameObject tempAudio = GameObject.Find("TempAudio");
                StopClipWithDelay(currentPlayingIndex, tempAudio, 0); // Hemen durdur
                await Task.Delay(100); // Durdurma iþleminin tamamlanmasý için kýsa bir gecikme
            }

            // Eðer týklanan ses zaten çalýyorsa durdur
            if (isPlaying[index])
            {
                GameObject tempAudio = GameObject.Find("TempAudio");
                StopClipWithDelay(index, tempAudio, 0);
                return; // Ses durdurulduktan sonra baþka bir iþlem yapýlmasýn
            }

            // Çalma durumu güncellendiðinde buton durumu doðru kalacak
            isPlaying[index] = true;
            currentPlayingIndex = index;

            AudioType audioType = GetAudioTypeFromURL(url);

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error downloading audio clip: {www.error}. URL: {url}");
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                    if (clip != null)
                    {
                        // Geçici bir ses objesi oluþtur
                        GameObject tempAudio = new GameObject("TempAudio");
                        AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
                        audioSource.clip = clip;
                        audioSource.Play();

                        // Çalma süresi boyunca bekle
                        await Task.Delay((int)(clip.length * 1000));

                        // Ses bittiðinde durdur
                        StopClipWithDelay(index, tempAudio, 0);
                    }
                }
            }
        }

        private AudioType GetAudioTypeFromURL(string url)
        {
            if (url.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.WAV;
            }
            else if (url.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.MPEG;
            }
            else if (url.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.OGGVORBIS;
            }
            else
            {
                return AudioType.UNKNOWN; // Bilinmeyen bir ses formatý
            }
        }

        private async Task DownloadAndPlayClip(string url)
        {
            AudioType audioType = GetAudioTypeFromURL(url);

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))  // AudioType'ý ihtiyacýnýza göre ayarlayýn
            {
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error downloading audio clip: {www.error}. URL: {url}");
                }
                else
                {
                    // Ses dosyasýný indir ve AudioClip olarak al
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                    if (clip != null)
                    {
                        PlayClipUsingAudioSource(clip);
                    }
                    else
                    {
                        Debug.LogError("Failed to load AudioClip from URL or unsupported audio format.");
                    }
                }
            }
        }

        private void PlayClipUsingAudioSource(AudioClip clip)
        {
            if (clip != null)
            {
                // Geçici bir oyun objesi oluþtur
                GameObject tempAudio = new GameObject("TempAudio");
                AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
                audioSource.clip = clip;
                audioSource.Play();

                // Sesin süresi kadar bekle, sonra nesneyi yok et
                StopClipWithDelay(0, tempAudio, clip.length);
            }
            else
            {
                Debug.LogError("AudioClip is null, cannot play sound.");
            }
        }

        private void StopClipWithDelay(int index, GameObject obj, float delay)
        {
            if (delay == 0 || obj == null)
            {
                // Ses çalmayý durdur ve durdurulduðunu iþaretle
                isPlaying[index] = false;

                // Oynatýlan ses objesini yok et
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }

                // WaitAndDestroy iþleminden çýk (aboneliði sonlandýr)
                if (destructionAction != null)
                {
                    UnityEditor.EditorApplication.update -= destructionAction;
                    destructionAction = null;
                }

                // currentPlayingIndex'i sýfýrla
                if (currentPlayingIndex == index)
                {
                    currentPlayingIndex = -1;
                }
            }
        }

        //Dosya boyutu öðrenme
        private async Task<string> GetFileSizeFromURL(string url)
        {
            using (UnityWebRequest www = UnityWebRequest.Head(url))
            {
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error fetching file size: {www.error}. URL: {url}");
                    return "Unknown";
                }
                else
                {
                    string size = www.GetResponseHeader("Content-Length");
                    if (!string.IsNullOrEmpty(size) && long.TryParse(size, out long sizeInBytes))
                    {
                        // Eðer dosya boyutu 1 MB'dan büyükse MB cinsinden göster, deðilse KB cinsinden göster
                        if (sizeInBytes >= 1024 * 1024)
                        {
                            return $"{(sizeInBytes / (1024f * 1024f)):F2} MB";
                        }
                        else
                        {
                            return $"{(sizeInBytes / 1024f):F2} KB";
                        }
                    }
                    else
                    {
                        return "Unknown";
                    }
                }
            }
        }
    }
}