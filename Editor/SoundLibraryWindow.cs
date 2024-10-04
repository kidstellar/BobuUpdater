namespace BobuEditor
{
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using UnityEngine.Networking;
    using System.Threading.Tasks;
    using System.IO;
    using System.Net.Http;
    using System;
    using UnityEditor.PackageManager;
    using System.Net;

    public class SoundLibraryWindow : EditorWindow
    {
        string packagePath = "Packages/com.kidstellar.bobuupdater/Editor/SoundsDataOBJ"; // Paket i�indeki Sounds klas�r�n�n yolu

        private string searchQuery = ""; // Ses arama kutucu�u de�i�keni
        private SoundTag tagFilter = SoundTag.All;  // T�r ile arama
        private SubSoundTag subTagFilter = SubSoundTag.None;  // Alt t�r i�in filtre
        private SoundObject[] allSounds;
        private Vector2 scrollPos;

        private bool[] isDownloading; // Hangi seslerin indirildi�ini izler
        private bool[] isDownloaded; // Hangi seslerin indirildi�ini izler
        private int currentPlayingIndex = -1; // �u anda �alan sesin indeksini tutar (-1: Hi�biri �alm�yor)
        private bool[] isPlaying; // Her sesin �alma durumunu izler

        private bool loaded = false;

        private float rotationSpeed = 0.5f; // D�n�� h�z�n� kontrol eder (daha d���k de�er, daha yava� d�n��)
        private float rotationAngle = 0f;  // �emberin a��s�
        private Texture2D loadingSpinner; // Y�kleme �emberi g�rseli

        private UnityEditor.EditorApplication.CallbackFunction destructionAction = null;

        [MenuItem("Bobu/Sound Library")]
        public static void ShowWindow()
        {
            var window = GetWindow<SoundLibraryWindow>("Sound Library");

            // Pencerenin minimum ve maksimum boyutunu ayarla
            window.minSize = new Vector2(560, 600); // Minimum boyut
            window.maxSize = new Vector2(560, 600); // Maksimum boyut
        }

        private void OnEnable()
        {

            //string packageP = "Packages/com.kidstellar.bobuupdater/Textures/spinner.png";
            //loadingSpinner = AssetDatabase.LoadAssetAtPath<Texture2D>(packageP);

            loadingSpinner = EditorGUIUtility.Load("Assets/BobuUpdater/Editor/Textures/spinner.png") as Texture2D;

            Debug.Log(loadingSpinner != null);

            // SoundObject varl�klar�n� paket i�inde arama
            string[] guids = AssetDatabase.FindAssets("t:SoundObject", new[] { packagePath });

            if (guids.Length > 0)
            {
                // Paket i�inde ses dosyalar� bulundu, bunlar� y�kl�yoruz
                allSounds = new SoundObject[guids.Length];

                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    allSounds[i] = AssetDatabase.LoadAssetAtPath<SoundObject>(assetPath);
                }
            }
            else
            {
                string[] gui = AssetDatabase.FindAssets("t:SoundObject");
                allSounds = gui.Select(guid => AssetDatabase.LoadAssetAtPath<SoundObject>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            }

            isDownloading = new bool[allSounds.Length]; // �ndirme i�lemi s�ren sesler
            isDownloaded = new bool[allSounds.Length];  // �ndirilen sesler

            isPlaying = new bool[allSounds.Length]; // T�m seslerin �alma durumu ba�lang��ta false olacak (yani �alm�yor)

            loaded = true;
        }

        private void OnGUI()
        {
            if (loaded)
            {
                GUILayout.Label("Sound Library", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();

                // Arama Bar�
                GUILayout.Label("Search:", GUILayout.Width(50));
                searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.Width(200));

                // T�r filtresi
                GUILayout.Label("Type:", GUILayout.Width(30));
                tagFilter = (SoundTag)EditorGUILayout.EnumPopup(tagFilter, GUILayout.Width(100));

                // Alt t�r filtresi
                GUILayout.Label("Subspecies:", GUILayout.Width(50));
                subTagFilter = (SubSoundTag)EditorGUILayout.EnumPopup(subTagFilter, GUILayout.Width(100));

                GUILayout.EndHorizontal();

                // D�� alan stili
                GUIStyle outerBoxStyle = new GUIStyle(GUI.skin.box);
                outerBoxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 1f));  // Koyu d�� alan rengi

                // ScrollView i�in d�� alan
                GUILayout.BeginVertical(outerBoxStyle);

                // Scrollable alan
                scrollPos = GUILayout.BeginScrollView(scrollPos);

                // Filtrelenmi� sesleri listele
                var filteredSounds = allSounds
                    .Where(sound => string.IsNullOrEmpty(searchQuery) || sound.soundName.ToLower().Contains(searchQuery.ToLower()))
                    .Where(sound => tagFilter == SoundTag.All || sound.tag == tagFilter)  // All se�ene�inde filtreyi atla
                    .Where(sound => subTagFilter == SubSoundTag.None || sound.subTag == subTagFilter)  // Alt t�r filtreleme
                    .OrderBy(sound => sound.soundName)  // A-Z s�ralamas�
                    .ToList();

                for (int i = 0; i < filteredSounds.Count; i++)
                {
                    var sound = filteredSounds[i];

                    // �ndekse g�re farkl� renkler
                    Color bgColor = i % 2 == 0 ? new Color(0.2f, 0.2f, 0.2f, 1f) : new Color(0.15f, 0.15f, 0.15f, 1f);
                    GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                    boxStyle.normal.background = MakeTex(2, 2, bgColor);

                    GUILayout.BeginHorizontal(boxStyle);

                    // Ses ad� i�in sabit geni�lik ayar�
                    GUILayout.Label(sound.soundName, GUILayout.Width(320));

                    // E�er indirme i�lemi s�r�yorsa indirme simgesi g�ster
                    if (isDownloading[i])
                    {
                        GUILayout.Label(EditorGUIUtility.IconContent("d_Progress"), GUILayout.Width(20), GUILayout.Height(20));
                    }
                    // E�er indirme tamamland�ysa tik simgesi g�ster
                    else if (isDownloaded[i])
                    {
                        GUILayout.Label(EditorGUIUtility.IconContent("d_FilterSelectedOnly"), GUILayout.Width(20), GUILayout.Height(20));
                    }
                    else
                    {
                        GUILayout.Space(25); // Simge i�in bo�luk
                    }

                    // Dinle/Durdur butonu
                    string buttonLabel = isPlaying[i] ? "Durdur" : "Dinle";
                    if (GUILayout.Button(buttonLabel, GUILayout.Width(80)))
                    {
                        PlayClipFromURL(sound.downloadLink, i); // Sesin URL'sini ve indeksini vererek �alma/durdurma i�lemini yap
                    }

                    // �ndirme butonu i�in sabit geni�lik ayar�
                    if (GUILayout.Button("�ndir", GUILayout.Width(80)))
                    {
                        isDownloading[i] = true;
                        DownloadAndSaveClip(sound.downloadLink, sound.soundName, i);  // �ndirme durumu ve index ile
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                GUILayout.EndVertical();  // D�� alan� kapat 
            }
            else
            {
                // Y�kleniyor ekran�
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();

                // D�nen �emberi �iz
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                // �emberi d�nd�r
                DrawLoadingSpinner();

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                // �emberin d�n�� a��s�n� g�ncelle (zamanla orant�l� bir �ekilde)
                rotationAngle += rotationSpeed * Time.deltaTime;

                // E�er a�� 360 dereceden b�y�kse s�f�rla (sonsuz d�ng� i�in)
                if (rotationAngle > 360f)
                {
                    rotationAngle -= 360f;
                }

                Repaint(); // Yeniden �izim
            }
        }

        private void DrawLoadingSpinner()
        {
            if (loadingSpinner != null)
            {
                float spinnerWidth = 100f;
                float spinnerHeight = 100f;

                // G�rselin merkezini hesaplay�n
                float pivotX = Screen.width / 2;
                float pivotY = Screen.height / 2;

                // GUI d�n�� i�lemi �ncesi matrix'i kaydedin
                Matrix4x4 matrixBackup = GUI.matrix;

                // G�rselin d�n���n� merkez noktas�nda sa�lay�n
                GUIUtility.RotateAroundPivot(rotationAngle, new Vector2(pivotX, pivotY));

                // G�rseli ekranda merkezleyin
                GUI.DrawTexture(new Rect(pivotX - spinnerWidth / 2, pivotY - spinnerHeight / 2, spinnerWidth, spinnerHeight), loadingSpinner);

                // GUI matrix'ini geri y�kleyin
                GUI.matrix = matrixBackup;
            }
        }

        private async void DownloadAndSaveClip(string url, string fileName, int index)
        {
            string folderPath = Path.Combine(Application.dataPath, "Sounds");

            // Sounds klas�r�n� olu�tur (e�er yoksa)
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
                    // �ndirilen veriyi byte dizisi olarak al
                    byte[] fileData = www.downloadHandler.data;

                    // Dosyay� belirtilen yola kaydet
                    File.WriteAllBytes(fullPath, fileData);
                    Debug.Log($"Dosya kaydedildi: {fullPath}");

                    // Dosya sistemini yenile (Unity'nin dosyalar� g�rmesi i�in)
                    AssetDatabase.Refresh();

                    // �ndirme tamamland�, indirme simgesini kapat ve tik i�aretini g�ster
                    isDownloading[index] = false;
                    isDownloaded[index] = true;

                    // Tik i�aretini belirli bir s�re g�ster, ard�ndan kaybolsun
                    await Task.Delay(2000); // 2 saniye bekle
                    isDownloaded[index] = false;
                }
            }
        }

        // Texture olu�turmak i�in bir yard�mc� fonksiyon
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
            // E�er ba�ka bir ses �al�yorsa, �nce onu durdur
            if (currentPlayingIndex != -1 && currentPlayingIndex != index)
            {
                GameObject tempAudio = GameObject.Find("TempAudio");
                StopClipWithDelay(currentPlayingIndex, tempAudio, 0); // Hemen durdur
                await Task.Delay(100); // Durdurma i�leminin tamamlanmas� i�in k�sa bir gecikme
            }

            // E�er t�klanan ses zaten �al�yorsa durdur
            if (isPlaying[index])
            {
                GameObject tempAudio = GameObject.Find("TempAudio");
                StopClipWithDelay(index, tempAudio, 0);
                return; // Ses durdurulduktan sonra ba�ka bir i�lem yap�lmas�n
            }

            // �alma durumu g�ncellendi�inde buton durumu do�ru kalacak
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
                        // Ge�ici bir ses objesi olu�tur
                        GameObject tempAudio = new GameObject("TempAudio");
                        AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
                        audioSource.clip = clip;
                        audioSource.Play();

                        // �alma s�resi boyunca bekle
                        await Task.Delay((int)(clip.length * 1000));

                        // Ses bitti�inde durdur
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
                return AudioType.UNKNOWN; // Bilinmeyen bir ses format�
            }
        }

        private void StopClipWithDelay(int index, GameObject obj, float delay)
        {
            if (delay == 0 || obj == null)
            {
                // Ses �almay� durdur ve durduruldu�unu i�aretle
                isPlaying[index] = false;

                // Oynat�lan ses objesini yok et
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }

                // WaitAndDestroy i�leminden ��k (aboneli�i sonland�r)
                if (destructionAction != null)
                {
                    UnityEditor.EditorApplication.update -= destructionAction;
                    destructionAction = null;
                }

                // currentPlayingIndex'i s�f�rla
                if (currentPlayingIndex == index)
                {
                    currentPlayingIndex = -1;
                }
            }
        }
    }
}