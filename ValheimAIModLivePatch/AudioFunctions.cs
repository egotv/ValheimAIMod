using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        private static AudioClip recordedAudioClip;
        public static bool IsRecording = false;
        private static float recordingStartedTime = 0f;
        private static bool shortRecordingWarningShown = false;

        private int recordingLength = 10; // Maximum recording length in seconds
        private int sampleRate = 22050; // Reduced from 44100
        private int bitDepth = 8; // Reduced from 16

        private void StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "No microphone detected! Please connect a microphone and restart the game.");
                return;
            }

            string micName = null;
            if (instance.MicrophoneIndex < 0 || instance.MicrophoneIndex >= Microphone.devices.Count())
                micName = Microphone.devices[instance.MicrophoneIndex];
            recordedAudioClip = Microphone.Start(micName, false, recordingLength, sampleRate);
            IsRecording = true;
            recordingStartedTime = Time.time;
            AddChatTalk(Player.m_localPlayer, Player.m_localPlayer.GetPlayerName(), "...");
            //Debug.Log("Recording started");
        }

        private void StopRecording()
        {
            // Stop the audio recording
            Microphone.End(null);
            IsRecording = false;
            //Debug.Log("Recording stopped");

            TrimSilence();

            SaveRecording();

            Chat.WorldTextInstance oldtext = Chat.instance.FindExistingWorldText(99991);
            if (oldtext != null && oldtext.m_gui)
            {
                UnityEngine.Object.Destroy(oldtext.m_gui);
                Chat.instance.m_worldTexts.Remove(oldtext);
            }
        }

        private void TrimSilence()
        {
            float[] samples = new float[recordedAudioClip.samples];
            recordedAudioClip.GetData(samples, 0);

            int lastNonZeroIndex = samples.Length - 1;
            while (lastNonZeroIndex > 0 && samples[lastNonZeroIndex] == 0)
            {
                lastNonZeroIndex--;
            }

            Array.Resize(ref samples, lastNonZeroIndex + 1);

            AudioClip trimmedClip = AudioClip.Create("TrimmedRecording", samples.Length, recordedAudioClip.channels, 44100, false);
            trimmedClip.SetData(samples, 0);

            recordedAudioClip = trimmedClip;
        }

        private byte[] EncodeToWav(AudioClip clip)
        {
            float[] samples = new float[clip.samples];
            clip.GetData(samples, 0);

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // RIFF header
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(36 + samples.Length * (bitDepth / 8));
                    writer.Write("WAVE".ToCharArray());

                    // Format chunk
                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16);
                    writer.Write((ushort)1); // Audio format (1 = PCM)
                    writer.Write((ushort)clip.channels);
                    writer.Write(sampleRate);
                    writer.Write(sampleRate * clip.channels * (bitDepth / 8)); // Byte rate
                    writer.Write((ushort)(clip.channels * (bitDepth / 8))); // Block align
                    writer.Write((ushort)bitDepth); // Bits per sample

                    // Data chunk
                    writer.Write("data".ToCharArray());
                    writer.Write(samples.Length * (bitDepth / 8));

                    // Convert float samples to 8-bit PCM
                    if (bitDepth == 8)
                    {
                        foreach (float sample in samples)
                        {
                            writer.Write((byte)((sample + 1f) * 127.5f));
                        }
                    }
                    else // 16-bit PCM
                    {
                        foreach (float sample in samples)
                        {
                            writer.Write((short)(sample * 32767));
                        }
                    }
                }
                return stream.ToArray();
            }
        }

        private void SaveRecording()
        {
            byte[] wavData = EncodeToWav(recordedAudioClip);

            try
            {
                File.WriteAllBytes(playerDialogueAudioPath, wavData);
                //Debug.Log("Recording saved to: " + playerDialogueAudioPath);
            }
            catch (Exception e)
            {
                LogError("Error saving recording: " + e.Message);
            }
        }

        private AudioClip LoadAudioClip(string audioPath)
        {
            AudioClip loadedClip;

            if (File.Exists(audioPath))
            {
                byte[] audioData = File.ReadAllBytes(audioPath);

                // Read the WAV file header to determine the audio format
                int channels = BitConverter.ToInt16(audioData, 22);
                int frequency = BitConverter.ToInt32(audioData, 24);

                // Convert the audio data to float[] format
                int headerSize = 44; // WAV header size is typically 44 bytes
                int dataSize = audioData.Length - headerSize;
                float[] floatData = new float[dataSize / 4];
                for (int i = 0; i < floatData.Length; i++)
                {
                    floatData[i] = BitConverter.ToSingle(audioData, i * 4 + headerSize);
                }

                bool stream = false;
                loadedClip = AudioClip.Create("AudioClipName", floatData.Length / channels, channels, frequency, stream);
                loadedClip.SetData(floatData, 0);
                //Debug.Log("AudioClip loaded successfully.");
                return loadedClip;
            }
            else
            {
                LogError("Audio file not found: " + audioPath);
                return null;
            }
        }

        private void PlayRecordedAudio(string fileName)
        {
            /*string audioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue_raw.wav");
            AudioClip audioClip = LoadAudioClip(audioPath);*/
            AudioClip recordedClip = LoadAudioClip(playerDialogueAudioPath);
            AudioClip downloadedClip = LoadAudioClip(npcDialogueAudioPath);

            if (recordedClip && downloadedClip)
                CompareAudioFormats(recordedClip, downloadedClip);

            if (recordedClip != null)
                AudioSource.PlayClipAtPoint(recordedClip, Player.m_localPlayer.transform.position, 1f);

            LogInfo("Playing last recorded clip audio");
        }

        public void MyPlayAudio(AudioClip clip)
        {
            GameObject gameObject = new GameObject("One shot audio");
            gameObject.transform.position = Player.m_localPlayer.transform.position;
            AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
            audioSource.clip = clip;
            audioSource.spatialBlend = 0f;
            audioSource.volume = (instance.npcVolume / 100);
            audioSource.bypassEffects = true;
            audioSource.bypassListenerEffects = true;
            audioSource.bypassReverbZones = true;
            //audioSource.Play();
            audioSource.PlayOneShot(clip, 5);
            UnityEngine.Object.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
        }

        public void PlayWavFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LogError($"File not found: {filePath}");
                return;
            }

            try
            {
                byte[] wavData = File.ReadAllBytes(filePath);
                AudioClip clip = WavToAudioClip(wavData, Path.GetFileNameWithoutExtension(filePath));

                MyPlayAudio(clip);
            }
            catch (Exception e)
            {
                LogError($"Error playing WAV file: {e.Message}");
            }
        }

        private AudioClip WavToAudioClip(byte[] wavData, string clipName)
        {
            // Parse WAV header
            int channels = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int bitsPerSample = BitConverter.ToInt16(wavData, 34);

            //Debug.Log($"Channels: {channels}, Sample Rate: {sampleRate}, Bits per Sample: {bitsPerSample}");

            // Find data chunk
            int dataChunkStart = 12; // Start searching after "RIFF" + size + "WAVE"
            while (!(wavData[dataChunkStart] == 'd' && wavData[dataChunkStart + 1] == 'a' && wavData[dataChunkStart + 2] == 't' && wavData[dataChunkStart + 3] == 'a'))
            {
                dataChunkStart += 4;
                int chunkSize = BitConverter.ToInt32(wavData, dataChunkStart);
                dataChunkStart += 4 + chunkSize;
            }
            int dataStart = dataChunkStart + 8;

            // Extract audio data
            float[] audioData = new float[(wavData.Length - dataStart) / (bitsPerSample / 8)];

            for (int i = 0; i < audioData.Length; i++)
            {
                if (bitsPerSample == 16)
                {
                    short sample = BitConverter.ToInt16(wavData, dataStart + i * 2);
                    audioData[i] = sample / 32768f;
                }
                else if (bitsPerSample == 8)
                {
                    audioData[i] = (wavData[dataStart + i] - 128) / 128f;
                }
            }

            AudioClip audioClip = AudioClip.Create(clipName, audioData.Length / channels, channels, sampleRate, false);
            audioClip.SetData(audioData, 0);

            return audioClip;
        }

        private void CompareAudioFormats(AudioClip firstClip, AudioClip secondClip)
        {
            // Check the audio format of the recorded clip
            Debug.Log("First Clip:");
            Debug.Log("Channels: " + firstClip.channels);
            Debug.Log("Frequency: " + firstClip.frequency);
            Debug.Log("Samples: " + firstClip.samples);
            Debug.Log("Length: " + firstClip.length);

            // Check the audio format of the loaded clip
            Debug.Log("Second Clip:");
            Debug.Log("Channels: " + secondClip.channels);
            Debug.Log("Frequency: " + secondClip.frequency);
            Debug.Log("Samples: " + secondClip.samples);
            Debug.Log("Length: " + secondClip.length);
        }

        private string GetBase64FileData(string audioPath)
        {
            if (File.Exists(audioPath))
            {
                byte[] audioData = File.ReadAllBytes(audioPath);
                string base64AudioData = Convert.ToBase64String(audioData);

                return base64AudioData;
            }
            else
            {
                LogError("Audio file not found: " + audioPath);
                return null;
            }
        }
    }
}
