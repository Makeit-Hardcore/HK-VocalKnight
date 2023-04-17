using System;
using System.IO;
using System.Threading.Tasks;
using VocalKnight.Extensions;
using Whisper.net;
using UnityEngine;
using System.Collections;
using UnityEngine.Profiling;
using System.Data;
using Whisper.net.Ggml;
using System.Security.Cryptography;

namespace VocalKnight.Utils
{
    public class WhisperUtil
    {
        private WhisperFactory factory;
        private WhisperProcessorBuilder processorBuilder;
        private WhisperProcessor processor;

        private readonly int sampleTime = 5;

        AudioClip segment;

        private MonoBehaviour _coroutineRunner;

        public WhisperUtil()
        {
            Logger.Log("Initializing whisper util");

            byte[] modelStream = File.ReadAllBytes("E:\\Hollow Knight 1.5 Modded\\hollow_knight_Data\\Managed\\Mods\\VocalKnight\\ggml-base.bin");

            factory = WhisperFactory.FromBuffer(modelStream);
            processorBuilder = factory.CreateBuilder().WithLanguage("en");
            processor = processorBuilder.Build();

            var go = new GameObject();
            GameObject.DontDestroyOnLoad(go);
            _coroutineRunner = go.AddComponent<NonBouncer>();
            _coroutineRunner.StartCoroutine(PollRecording());

            segment = Microphone.Start(null, false, sampleTime, 44100);
        }

        private async Task ProcessAudio(Stream data)
        {
            await foreach (var segment in processor.ProcessAsync(data))
            {
                Logger.Log("New segment: " + segment.Text);
            }
        }

        private IEnumerator PollRecording()
        {
            for (float timer = 0.05f; timer > 0; timer -= Time.deltaTime)
            {
                yield return null;
            }

            if (!Microphone.IsRecording(null))
            {
                float[] audioData = new float[segment.samples];
                segment.GetData(audioData, 0);

                byte[] byteData = new byte[audioData.Length * 2];
                convertData(byteData, audioData);

                MemoryStream wavStream = new MemoryStream();
                wavStream.Write(byteData, 0, byteData.Length);

                ProcessAudio(wavStream).Wait();

                segment = Microphone.Start(null, false, sampleTime, 48000);
            }
            yield return PollRecording();
        }

        // The following code taken from SavWav.cs, created by Calvin Rien of The Darktable
        private void convertData(byte[] dataOut, float[] samples)
        {
            Int16[] intData = new Int16[samples.Length];
            //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

            dataOut = new Byte[samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                Byte[] byteArr = new Byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(dataOut, i * 2);
            }
        }
    }
}

