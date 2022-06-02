using System;

namespace NoisesLibrary
{
    public class NoisesClass
    {
        private struct VectorXY
        {
            public VectorXY(float X, float Y)
            {
                x = X;
                y = Y;
            }
            public float x;
            public float y;
        }
        //Массив случайных чисел
        private byte[] permutationTable;
        private int w, h;
        //Карта шума
        private float[] noiseMap;

        /// <summary>
        /// Возвращает двумерную карту высот созданую с помощью шума перлина
        /// </summary>
        /// <param name="width">Ширина</param>
        /// <param name="height">Высота</param>
        /// <param name="seedOctaveShift">Набор псевдослучайныхчисел для октав</param>
        /// <param name="seedPermutationTable">Набор псевдослучайныхчисел для таблицы псевдослучайныхчисел</param>
        /// <param name="scale">Размерность</param>
        /// <param name="octaves">Кол-во октав</param>
        /// <param name="persistence">Контроль изменений амплитуды</param>
        /// <param name="lacunarity">Контроль изменений частоты</param>
        /// <param name="startX">Начальная точка Х</param>
        /// <param name="startY">Начальная точка У</param>
        /// <returns></returns>
        public float[] PerlineNoise(int width, int height, int seedOctaveShift = 1, int seedPermutationTable = 1, float scale = 100, int octaves = 3, float persistence = 0.5f, float lacunarity = 2, float startX = 0, float startY = 0)
        {
            w = width;
            h = height;
            //Массив данных о вершинах
            noiseMap = new float[w * h];

            Perlin2D(seedPermutationTable);

            VectorXY offset = new VectorXY(startX, startY);

            //Порождающий элемент
            System.Random rand = new System.Random(seedOctaveShift);

            //Сдвиг октав
            VectorXY[] octavesOffset = new VectorXY[octaves];
            for (int i = 0; i < octaves; i++)
            {
                float xOffset = rand.Next(-100000, 100000) + offset.x;
                float yOffset = rand.Next(-100000, 100000) + offset.y;
                octavesOffset[i] = new VectorXY(xOffset / w, yOffset / h);
            }

            if (scale < 0)
            {
                scale = 0.0001f;
            }

            //Учитываем половину ширины и высоты
            float halfWidth = w / 2f;
            float halfHeight = h / 2f;

            //Генерируем точки на карте высот
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    //Задаём значения для первой октавы
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;
                    float superpositionCompensation = 0;

                    //Обработка наложения октав
                    for (int i = 0; i < octaves; i++)
                    {
                        //Рассчет координаты для получения значения из шума перлина
                        float xResult = (x - halfWidth) / scale * frequency + octavesOffset[i].x * frequency;
                        float yResult = (y - halfHeight) / scale * frequency + octavesOffset[i].y * frequency;

                        //Получение высоты из шума
                        float generatedValue = NoisePerline(xResult, yResult);
                        //Наложение октав
                        noiseHeight += generatedValue * amplitude;
                        //Компенсируем наложение октав чтобы остаться в границах диапазона [-1,1]
                        noiseHeight -= superpositionCompensation;

                        // Расчёт амплитуды, частоты и компенсации для следующей октавы
                        amplitude *= persistence;
                        frequency *= lacunarity;
                        superpositionCompensation = amplitude / 2;
                    }


                    // Из-за наложения октав есть вероятность выхода за границы диапазона [-1,1]
                    if (noiseHeight < -1.0f)
                        noiseHeight = -1.0f;
                    if (noiseHeight > 1.0f)
                        noiseHeight = 1.0f;

                    // Сохраняем точку для карты высот
                    noiseMap[y * w + x] = (noiseHeight + 1) / 2;
                }
            }

            return noiseMap;
        }

        private void Perlin2D(int seed = 0)
        {
            //Таблица со случайными числами
            var rand = new System.Random(seed);
            permutationTable = new byte[w * h];
            rand.NextBytes(permutationTable);
        }

        private float[] GetPseudoRandomGradientVector(int x, int y)
        {
            //Хэш-функция с простыми числами
            int v = (int)(((x * 1836311903) ^ (y * 2971215073) + 4807526976) & 1023);
            v = permutationTable[v] & 3;
            //Выбор градиентного вектора
            switch (v)
            {
                case 0: return new float[] { 1, 0 };
                case 1: return new float[] { -1, 0 };
                case 2: return new float[] { 0, 1 };
                default: return new float[] { 0, -1 };
            }
        }

        private float QunticCurve(float t)
        {
            //Билинейная интерполяция
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Lerp(float a, float b, float t)
        {
            //Линейная интерполяция
            return a + (b - a) * t;
        }

        private float Dot(float[] a, float[] b)
        {
            //Cкалярное произведение векторов
            return a[0] * b[0] + a[1] * b[1];
        }

        private float NoisePerline(float fx, float fy)
        {
            //Координаты левой верхней вершины квадрата
            int left = (int)System.Math.Floor(fx);
            int top = (int)System.Math.Floor(fy);
            //Локальные координаты точки внутри квадрата
            float pointInQuadX = fx - left;
            float pointInQuadY = fy - top;
            //Получение градиентных векторов для всех вершин квадрата
            float[] topLeftGradient = GetPseudoRandomGradientVector(left, top);
            float[] topRightGradient = GetPseudoRandomGradientVector(left + 1, top);
            float[] bottomLeftGradient = GetPseudoRandomGradientVector(left, top + 1);
            float[] bottomRightGradient = GetPseudoRandomGradientVector(left + 1, top + 1);
            //Вектора от вершин квадрата до точки внутри квадрата
            float[] distanceToTopLeft = new float[] { pointInQuadX, pointInQuadY };
            float[] distanceToTopRight = new float[] { pointInQuadX - 1, pointInQuadY };
            float[] distanceToBottomLeft = new float[] { pointInQuadX, pointInQuadY - 1 };
            float[] distanceToBottomRight = new float[] { pointInQuadX - 1, pointInQuadY - 1 };
            //Скалярные произведения
            float tx1 = Dot(distanceToTopLeft, topLeftGradient);
            float tx2 = Dot(distanceToTopRight, topRightGradient);
            float bx1 = Dot(distanceToBottomLeft, bottomLeftGradient);
            float bx2 = Dot(distanceToBottomRight, bottomRightGradient);
            //Параметры интерполяции
            pointInQuadX = QunticCurve(pointInQuadX);
            pointInQuadY = QunticCurve(pointInQuadY);
            //Интерполяция
            float tx = Lerp(tx1, tx2, pointInQuadX);
            float bx = Lerp(bx1, bx2, pointInQuadX);
            float tb = Lerp(tx, bx, pointInQuadY);

            return tb;
        }






        public static float[,] heighmap;
        //Определяет разницу высот
        private static float roughness = 1.0f;
        private static Random randds;
        public static float maxi, min;
        private static bool lrflag = false;

        /// <summary>
        /// Возвращает массив с картой высот созданой с помощью шума Diamond-Square
        /// </summary>
        /// <param name="degreeOfTwo">Для определения размерапо формуле 2^x+1</param>
        /// <param name="seed">Параметр определения напобора псевдо случайных чисил</param>
        /// <param name="rectangle">Если нужна прямоугольная карта true, если квадратная false</param>
        /// <param name="asperity">Определяет разницу высот, чем больше, тем более неравномерная карта высот</param>
        /// <param name="RMin">RandomStartChengeMin</param>
        /// <param name="RMax">RandomStartChengeMax</param>
        public float[,] DiamondSquareNoise(UInt16 degreeOfTwo, UInt16 seed, bool rectangle, float asperity, float RMin = 0.3f, float RMax = 0.6f)
        {
            randds = new Random(seed);
            if (RMin > RMax)
            {
                RMax += RMin;
                RMin = RMax - RMin;
                RMax -= RMin;
            }
            roughness = asperity;
            h = w = (int)Math.Pow(2, degreeOfTwo) + 1;


            //Начальные значения по краям карты
            if (rectangle)
            {
                w = h * 2 - 1;
                heighmap = new float[w, h];
                heighmap[h - 1, h - 1] = (float)(((randds.NextDouble()) * (RMax - RMin)) + RMin);
                heighmap[h - 1, 0] = (float)(((randds.NextDouble()) * (RMax - RMin)) + RMin);
            }
            else
            {
                heighmap = new float[w, h];
            }
            heighmap[0, 0] = (float)(((randds.NextDouble()) * (RMax - RMin)) + RMin);
            heighmap[0, h - 1] = (float)(((randds.NextDouble()) * (RMax - RMin)) + RMin);
            heighmap[w - 1, h - 1] = (float)(((randds.NextDouble()) * (RMax - RMin)) + RMin);
            heighmap[w - 1, 0] = (float)(((randds.NextDouble()) * (RMax - RMin)) + RMin);

            maxi = RMax;
            min = RMin;

            //Вызов основного метода
            for (int l = (h - 1) /*/ 2*/; l > 0; l /= 2)
                for (int x = 0; x < w - 1; x += l)
                {
                    if (x >= h - l)
                        lrflag = true;
                    else
                        lrflag = false;

                    for (int y = 0; y < h - 1; y += l)
                        DiamondSquare(x, y, x + l, y + l);
                }
            return heighmap;
        }

        private void DiamondSquare(int lx, int ly, int rx, int ry)
        {
            //Сторона текущего квадрата
            int l = (rx - lx) / 2;

            //Расчёт центра квадрата
            Square(lx, ly, rx, ry);

            //Расчёт углов ромба
            Diamond(lx, ly + l, l);
            Diamond(rx, ry - l, l);
            Diamond(rx - l, ry, l);
            Diamond(lx + l, ly, l);
        }

        private void Square(int lx, int ly, int rx, int ry)
        {
            //Сторона текущего квадрата
            int l = (rx - lx) / 2;

            float a, b, c, d;

            //Получаем углы текущего квадрата
            a = heighmap[lx, ly];              //  А-------C
            b = heighmap[lx, ry];              //  |       |
            c = heighmap[rx, ly];              //  |   c   |
            d = heighmap[rx, ry];              //  |       |        
            //Координаты центра квадрата       //  В-------D
            int centerX = lx + l;
            int centerY = ly + l;

            //Новая точка на основе среднего и случайного кофицента
            float z = l * 2 * roughness / h;
            heighmap[centerX, centerY] = (a + b + c + d) / 4 + (float)(randds.NextDouble() * (z - (-z)) + (-z));
            //Вычисляем общий диапазон
            if (maxi < heighmap[centerX, centerY])
                maxi = heighmap[centerX, centerY];
            if (min > heighmap[centerX, centerY])
                min = heighmap[centerX, centerY];
        }


        private void Diamond(int tgx, int tgy, int l)
        {
            float a, b, c, d;

            //Полчение углов ромба
            if (tgy - l >= 0)
                a = heighmap[tgx, tgy - l];                    //      A--------
            else                                               //      |        |
                a = heighmap[tgx, h - l];                      // B---t g----D  |
                                                               //      |        |
                                                               //      C--------
            if (tgx - l >= 0)
                b = heighmap[tgx - l, tgy];
            else
                if (lrflag)
                b = heighmap[w - l, tgy];
            else
                b = heighmap[h - l, tgy];


            if (tgy + l < h)
                c = heighmap[tgx, tgy + l];
            else
                c = heighmap[tgx, l];


            if (lrflag)
            {
                if (tgx + l < w)
                    d = heighmap[tgx + l, tgy];
                else
                    d = heighmap[l, tgy];
            }
            else
            {
                if (tgx + l < h)
                    d = heighmap[tgx + l, tgy];
                else
                    d = heighmap[l, tgy];
            }

            //Новая точка на основе среднего и случайного кофицента
            float z = l * 2 * roughness / h;
            heighmap[tgx, tgy] = (a + b + c + d) / 4 + (float)(randds.NextDouble() * (z - (-z)) + (-z));
            //Вычисляем общий диапазон
            if (maxi < heighmap[tgx, tgy])
                maxi = heighmap[tgx, tgy];
            if (min > heighmap[tgx, tgy])
                min = heighmap[tgx, tgy];
        }
    }
}
