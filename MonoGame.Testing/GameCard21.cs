﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MonoGame.Framework;
using MonoGame.Framework.Audio;
using MonoGame.Framework.Graphics;
using MonoGame.Framework.Input;
using MonoGame.Imaging;
using MonoGame.Imaging.Processing;

namespace MonoGame.Testing
{
    public class GameCard21 : Game
    {
        private GraphicsDeviceManager _graphicsManager;
        private SpriteBatch _spriteBatch;

        private Texture2D _pixel;
        private SpriteFont _spriteFont;

        private Viewport _lastViewport;

        private Dictionary<string, TextureRegion2D> _plainCardRegions;
        private Dictionary<string, TextureRegion2D> _hdCardRegions;

        public GameCard21()
        {
            SoundEffect.Initialize();

            _graphicsManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            IsMouseVisible = true;

            base.Initialize();

            _lastViewport = GraphicsDevice.Viewport;
            ViewportChanged(_lastViewport);
        }

        private void ViewportChanged(in Viewport viewport)
        {
        }

        private int _loadCount;
        private int _loadTotal;

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(stackalloc Color[] { Color.White });

            _spriteFont = Content.Load<SpriteFont>("consolas");

            Task.Run(() =>
            {
                try
                {
                    var textures = CreateCardAtlas(
                        (count, total) =>
                        {
                            _loadCount = count;
                            _loadTotal = total;
                        },
                        out var plainMap, out var hdMap);

                    _loadTotal = 0;

                    _plainCardRegions = GetCardRegions(plainMap, textures);
                    _hdCardRegions = GetCardRegions(hdMap, textures);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        private static Dictionary<string, TextureRegion2D> GetCardRegions(
            Dictionary<string, string> map, (PackState State, Texture2D Texture)[] textures)
        {
            var regions = new Dictionary<string, TextureRegion2D>(map.Count);
            foreach (var (key, value) in map)
            {
                foreach (var (state, texture) in textures)
                {
                    if (state.Entries.TryGetValue(value, out Rectangle bounds))
                    {
                        regions.Add(key, new TextureRegion2D(texture, bounds));
                        break;
                    }
                }
            }
            return regions;
        }

        public delegate void AtlasProgressDelegate(int count, int total);

        private (PackState State, Texture2D Texture)[] CreateCardAtlas(
            AtlasProgressDelegate? progress,
            out Dictionary<string, string> plainMap,
            out Dictionary<string, string> hdMap)
        {
            string cardsPath = Path.Combine(Content.RootDirectory, "Cards");
            var cardFiles = Directory.GetFiles(cardsPath);

            // plainMap will contain all basic textures at first.
            plainMap = new Dictionary<string, string>();
            foreach (string cardFile in cardFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(cardFile);
                plainMap.Add(fileName, cardFile);
            }

            // hdMap will contain a mix of basic and HD textures.
            // plainMap will not contain HD textures.
            hdMap = new Dictionary<string, string>(plainMap);
            foreach (string key in plainMap.Keys.ToArray())
            {
                string hdKey = key + "2";
                if (hdMap.Remove(hdKey, out string? hdFile))
                {
                    plainMap.Remove(hdKey);
                    hdMap.Remove(key);
                    hdMap.Add(key, hdFile);
                }
            }

            int maxTextureSize = Math.Min(GraphicsDevice.Capabilities.MaxTexture2DSize, 4096);
            var packStates = new List<PackState>();
            int stateIndex = 0;

            int index = 0;
            using var cardImages = GetImages(cardFiles).GetEnumerator();
            bool hasValue = false;
            do
            {
                if (stateIndex >= packStates.Count)
                    packStates.Add(new PackState(maxTextureSize, maxTextureSize));

                var state = packStates[stateIndex];
                state.Image.MutateBuffer(buffer =>
                {
                    int padding = 2;
                    int x = padding;
                    int y = padding;
                    int largestHeight = 0;

                    while (hasValue || cardImages.MoveNext())
                    {
                        hasValue = true;
                        var (file, rawImage) = cardImages.Current;

                        int width = rawImage.Width / 2;
                        int height = rawImage.Height / 2;

                        int remainingHeight = buffer.Height - y;
                        if (remainingHeight < height)
                        {
                            stateIndex++;
                            break;
                        }

                        int remainingWidth = buffer.Width - x;
                        if (remainingWidth < width)
                            goto NextRow;

                        using (rawImage)
                        using (var image = rawImage.ProcessRows(
                            x => x.Resize<Color, int>(new Size(width, height), 0, null)))
                        {
                            Image.LoadPixels(image, buffer.Crop(x, y));
                        }

                        index++;
                        progress?.Invoke(index, cardFiles.Length);

                        state.Entries.Add(file, new Rectangle(x, y, width, height));

                        x += width + padding;
                        if (height > largestHeight)
                            largestHeight = height;

                        hasValue = false;
                        if (x < buffer.Width)
                            continue;

                        NextRow:
                        int filledWidth = x - padding;
                        if (filledWidth > state.ActualWidth)
                            state.ActualWidth = filledWidth;

                        y += largestHeight + padding;
                        largestHeight = 0;
                        x = padding;
                    }

                    int filledHeight = y - padding;
                    if (filledHeight > state.ActualHeight)
                        state.ActualHeight = filledHeight;
                });
            }
            while (hasValue);

            var textures = packStates.Select(state =>
            {
                using (var img = state.Image)
                {
                    var tex = new Texture2D(GraphicsDevice, state.ActualWidth, state.ActualHeight);
                    tex.SetData(img, tex.Bounds);
                    return (state, tex);
                }
            }).ToArray();

            return textures;
        }

        public class PackState
        {
            public Image<Color> Image { get; }
            public Dictionary<string, Rectangle> Entries { get; }
            public int ActualWidth { get; set; }
            public int ActualHeight { get; set; }

            public PackState(int width, int height)
            {
                Image = Image<Color>.Create(width, height);
                Entries = new Dictionary<string, Rectangle>();
            }
        }

        public static IEnumerable<(string, Image<Color>)> GetImages(IEnumerable<string> files)
        {
            if (files == null)
                throw new ArgumentNullException(nameof(files));

            foreach (string file in files)
            {
                Image<Color>? img;
                using (var fs = File.OpenRead(file))
                    img = Image.Load<Color>(fs);

                if (img == null)
                    throw new InvalidDataException("Could not decode " + file);

                yield return (file, img);
            }
        }

        public class PlayState
        {
            public List<Card> ActiveCards { get; }

            public PlayState()
            {
                ActiveCards = new List<Card>();
            }

            public int GetScore()
            {
                int score = 0;
                foreach (var card in ActiveCards)
                    score += card.Value;
                return score;
            }
        }

        [Flags]
        public enum CardType
        {
            Unknown = 0,

            Constant = 1 << 0,
            Clubs = 1 << 1 | Constant,
            Diamonds = 1 << 2 | Constant,
            Hearts = 1 << 3 | Constant,
            Spades = 1 << 4 | Constant,

            Special = 1 << 5,
            Ace = 1 << 6 | Special,
            Jack = 1 << 7 | Special,
            King = 1 << 8 | Special,
            Queen = 1 << 9 | Special,
            Joker = 1 << 10 | Special
        }

        public class Card
        {
            public CardType Type { get; }
            public int Value { get; }

            public Card(CardType type, int value)
            {
                Type = type;
                Value = value;
            }
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(in FrameTime time)
        {
            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();

            var mouse = Mouse.GetState();

            if (_loadTotal != 0)
            {
                Window.TaskbarList.ProgressState = Framework.Utilities.TaskbarProgressState.Normal;
                Window.TaskbarList.SetProgressValue(_loadCount, _loadTotal);
            }
            else
            {
                Window.TaskbarList.ProgressState = Framework.Utilities.TaskbarProgressState.None;
            }

            base.Update(time);
        }

        protected override void Draw(in FrameTime time)
        {
            var currentViewport = GraphicsDevice.Viewport;
            if (_lastViewport != currentViewport)
            {
                ViewportChanged(currentViewport);
                _lastViewport = currentViewport;
            }

            GraphicsDevice.Clear(new Color(Color.DarkGreen * 0.333f, byte.MaxValue));

            if (_loadTotal != 0)
            {
                _spriteBatch.Begin();

                float progress = _loadCount / (float)_loadTotal;
                _spriteBatch.FillRectangle(
                    0, 0, currentViewport.Width * progress, 50, Color.Blue);

                _spriteBatch.End();
            }

            float seconds = (float)time.Total.TotalSeconds;

            _spriteBatch.Begin(
                sortMode: SpriteSortMode.FrontToBack,
                blendState: BlendState.NonPremultiplied);

            var cardRegions = _hdCardRegions;
            if (cardRegions != null)
            {
                float totalLength = 2000;
                float padding = totalLength / cards.Length;
                float length = totalLength - padding;

                for (int i = 0; i < cards.Length; i++)
                {
                    ref CardSprite card = ref cards[i];

                    card.lastTime = card.time;
                    card.time = ((seconds * 50) / (float)cards.Length + i / (float)cards.Length) % 1f;

                    if (card.time < card.lastTime)
                    {
                        card.name = cardNames[rng.Next(cardNames.Length)];
                    }

                    if (card.name == null)
                        continue;

                    var region = cardRegions[card.name];
                    var origin = new Vector2(region.Width / 2, region.Height / 2);

                    float offset = card.time * totalLength;
                    float clampedOffsetX = MathHelper.Clamp(offset, padding, length) / 2;
                    float clampedOffsetY = MathHelper.Clamp((offset * 3) % 300f, padding, length);

                    float alpha = offset / length;

                    var scale = new Vector2(1f);
                    _spriteBatch.Draw(
                        region,
                        new Vector2(clampedOffsetX, clampedOffsetY) + origin * scale,
                        new Color(Color.White, alpha),
                        0,
                        origin,
                        scale,
                        SpriteFlip.None,
                        offset);
                }
            }

            _spriteBatch.End();

            base.Draw(time);
        }

        Random rng = new Random();
        CardSprite[] cards = new CardSprite[300];

        string[] cardNames = new string[]
        {
            "10_of_clubs",
            "10_of_diamonds",
            "10_of_hearts",
            "10_of_spades",

            "9_of_clubs",
            "9_of_diamonds",
            "9_of_hearts",
            "9_of_spades",

            "8_of_clubs",
            "8_of_diamonds",
            "8_of_hearts",
            "8_of_spades",

            "7_of_clubs",
            "7_of_diamonds",
            "7_of_hearts",
            "7_of_spades",

            "6_of_clubs",
            "6_of_diamonds",
            "6_of_hearts",
            "6_of_spades",

            "5_of_clubs",
            "5_of_diamonds",
            "5_of_hearts",
            "5_of_spades",

            "4_of_clubs",
            "4_of_diamonds",
            "4_of_hearts",
            "4_of_spades",

            "3_of_clubs",
            "3_of_diamonds",
            "3_of_hearts",
            "3_of_spades",

            "2_of_clubs",
            "2_of_diamonds",
            "2_of_hearts",
            "2_of_spades",

            "ace_of_clubs",
            "ace_of_diamonds",
            "ace_of_hearts",
            "ace_of_spades"
        };

        struct CardSprite
        {
            public string name;
            public float time;
            public float lastTime;
        }
    }
}
