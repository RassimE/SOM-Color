using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace aiSomColor
{
	public partial class MainForm : Form
	{
		const int SizeOfInputVector = 3;
		const int NumIterations = 1000;

		const int NumCellsAcross = 64;
		const int NumCellsDown = 64;

		const double InitialLearningRate = 0.25;

		int _numTrainingSet;

		double[][] _randomTrainingSet;
		double[][] _gradualTrainingSet;
		double[][] _predefTrainingSet = //new double[][]
		{
			new double[]{1, 0, 0},		// red
			new double[]{0, 1, 0},		// green
			new double[]{0, 0, 1},		// blue
			new double[]{1, 1, 0},		// yellow
			new double[]{1, 0.4, 0.25},	// orange
			new double[]{1, 0, 1},		// purple
			new double[]{0, 0.5, 0.25},	// dk_green
			new double[]{0, 0, 0.5},	// dk_blue

			new double[]{0, 1, 1},		// cyan
			//new double[]{0, 0, 0},		// black
			//new double[]{1, 1, 1},		// white
			//new double[]{0.5, 0.5, 0.5},	// gray
		};

		Random _rnd;
		SOM _som;

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			pictureBox1.Image = (Image)(new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height, PixelFormat.Format32bppRgb));
			pictureBox2.Image = (Image)(new Bitmap(pictureBox2.ClientSize.Width, pictureBox2.ClientSize.Height, PixelFormat.Format32bppRgb));
			pictureBox3.Image = (Image)(new Bitmap(pictureBox3.ClientSize.Width, pictureBox3.ClientSize.Height, PixelFormat.Format32bppRgb));
			pictureBox4.Image = (Image)(new Bitmap(pictureBox4.ClientSize.Width, pictureBox4.ClientSize.Height, PixelFormat.Format32bppRgb));
			pictureBox5.Image = (Image)(new Bitmap(pictureBox5.ClientSize.Width, pictureBox5.ClientSize.Height, PixelFormat.Format32bppRgb));
			pictureBox6.Image = (Image)(new Bitmap(pictureBox6.ClientSize.Width, pictureBox6.ClientSize.Height, PixelFormat.Format32bppRgb));

			_rnd = new Random();
			_som = new SOM(NumCellsAcross, NumCellsDown, SizeOfInputVector);

			_numTrainingSet = _predefTrainingSet.GetUpperBound(0) + 1;//_predefTrainingSet.Length;

			_randomTrainingSet = new double[_numTrainingSet][];
			_gradualTrainingSet = new double[_numTrainingSet][];

			for (int i = 0; i < _numTrainingSet; i++)
			{
				_randomTrainingSet[i] = new double[SizeOfInputVector];
				_gradualTrainingSet[i] = new double[SizeOfInputVector];

				_randomTrainingSet[i][0] = 1 - _rnd.NextDouble();
				_randomTrainingSet[i][1] = 1 - _rnd.NextDouble();
				_randomTrainingSet[i][2] = 1 - _rnd.NextDouble();

				double v = (double)i / (_numTrainingSet - 1);
				_gradualTrainingSet[i][0] = v;
				_gradualTrainingSet[i][1] = v;
				_gradualTrainingSet[i][2] = v;
			}

			comboBox2.SelectedIndex = 0;
			comboBox1.SelectedIndex = 0;
		}

		private void comboBox1_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (_som.FinishedTraining())
			{
				_som.Init(comboBox1.SelectedIndex, NumIterations, InitialLearningRate);

				BitmapData lockBits;
				lockBits = ((Bitmap)(pictureBox1.Image)).LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);

				_som.RenderWeight(lockBits);

				((Bitmap)(pictureBox1.Image)).UnlockBits(lockBits);
				pictureBox1.Refresh();

				btnReinit.Enabled = comboBox1.SelectedIndex == 0 || comboBox2.SelectedIndex == 0;
			}
		}

		unsafe private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			btnReinit.Enabled = comboBox1.SelectedIndex == 0 || comboBox2.SelectedIndex == 0;

			BitmapData lockBits = ((Bitmap)(pictureBox2.Image)).LockBits(new Rectangle(0, 0, pictureBox2.Image.Width, pictureBox2.Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
			int s = Math.Abs(lockBits.Stride);
			Int32* pBase = (Int32*)lockBits.Scan0.ToPointer();

			double[][] trainingSet;
			if (comboBox2.SelectedIndex == 0)
				trainingSet = _randomTrainingSet;
			else if (comboBox2.SelectedIndex == 1)
				trainingSet = _gradualTrainingSet;
			else if (comboBox2.SelectedIndex == 2)
				trainingSet = _predefTrainingSet;
			else
			{
				for (int y = 0; y < lockBits.Height; y++)
				{
					Int32* pix = pBase + y * (s >> 2);

					for (int x = 0; x < lockBits.Width; x++)
					{
						int color = SOM.toRGB(_rnd.NextDouble(), _rnd.NextDouble(), _rnd.NextDouble());
						*pix++ = color;
					}
				}

				((Bitmap)(pictureBox2.Image)).UnlockBits(lockBits);
				pictureBox2.Refresh();

				return;
			}

			double barHeight = (double)lockBits.Height / _numTrainingSet;


			for (int y = 0; y < lockBits.Height; y++)
			{
				int i = (int)(y / barHeight);

				int color = SOM.toRGB(trainingSet[i]);

				Int32* pix = pBase + y * (s >> 2);

				for (int x = 0; x < lockBits.Width; x++)
					*pix++ = color;
			}

			((Bitmap)(pictureBox2.Image)).UnlockBits(lockBits);
			pictureBox2.Refresh();
		}

		private void btnReinit_Click(object sender, System.EventArgs e)
		{
			timer1.Enabled = false;

			if (comboBox1.SelectedIndex == 0)
			{
				_som.Init(comboBox1.SelectedIndex, NumIterations, InitialLearningRate);

				BitmapData lockBits;
				lockBits = ((Bitmap)(pictureBox1.Image)).LockBits(new Rectangle(0, 0, pictureBox1.Image.Width, pictureBox1.Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
				_som.RenderWeight(lockBits);
				((Bitmap)(pictureBox1.Image)).UnlockBits(lockBits);
				pictureBox1.Refresh();
			}

			if (comboBox2.SelectedIndex == 0)
			{
				for (int i = 0; i < _numTrainingSet; i++)
				{
					_randomTrainingSet[i][0] = 1 - _rnd.NextDouble();
					_randomTrainingSet[i][1] = 1 - _rnd.NextDouble();
					_randomTrainingSet[i][2] = 1 - _rnd.NextDouble();
				}

				comboBox2_SelectedIndexChanged(comboBox2, null);
			}
		}

		private void btnTrain_Click(object sender, System.EventArgs e)
		{
			if (_som.FinishedTraining())
			{
				if (!_som.Initialised())
				{
					_som.Init(comboBox1.SelectedIndex, NumIterations, InitialLearningRate);

					BitmapData lockBits;
					lockBits = ((Bitmap)(pictureBox1.Image)).LockBits(new Rectangle(0, 0, pictureBox1.Image.Width, pictureBox1.Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);

					_som.RenderWeight(lockBits);

					((Bitmap)(pictureBox1.Image)).UnlockBits(lockBits);
					pictureBox1.Refresh();
				}

				timer1.Enabled = true;
			}
		}

		private void btnNet_Click(object sender, EventArgs e)
		{

		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			BitmapData lockBits;
			lockBits = ((Bitmap)(pictureBox3.Image)).LockBits(new Rectangle(0, 0, pictureBox3.Image.Width, pictureBox3.Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
			_som.RenderWeight(lockBits);
			((Bitmap)(pictureBox3.Image)).UnlockBits(lockBits);
			pictureBox3.Refresh();

			lockBits = ((Bitmap)(pictureBox4.Image)).LockBits(new Rectangle(0, 0, pictureBox4.Image.Width, pictureBox4.Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
			_som.RenderWinner(lockBits);
			((Bitmap)(pictureBox4.Image)).UnlockBits(lockBits);
			pictureBox4.Refresh();

			lockBits = ((Bitmap)(pictureBox5.Image)).LockBits(new Rectangle(0, 0, pictureBox5.Image.Width, pictureBox5.Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
			double totalError = _som.RenderError(lockBits, 2);
			((Bitmap)(pictureBox5.Image)).UnlockBits(lockBits);
			pictureBox5.Refresh();

			lockBits = ((Bitmap)(pictureBox6.Image)).LockBits(new Rectangle(0, 0, pictureBox6.Image.Width, pictureBox6.Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
			double totalError2 = _som.RenderError(lockBits, 5, true);
			((Bitmap)(pictureBox6.Image)).UnlockBits(lockBits);
			pictureBox6.Refresh();

			if (!Train())
			{
				statusBar1.Text = "Iteration: " + _som.IterationCount().ToString("0000") +
					"   Learning Rate: " + _som.LearningRate().ToString("0.000000") +					//"0.000000"
					"   Neighbourhood Radius: " + _som.NeighbourhoodRadius().ToString("0.000000") +		//"0.000000"
					"   Influence: " + _som.Influence().ToString("0.000000");							//"0.000000"
			}
			//else if (pSOM.Phase() == 0 && comboBox1.SelectedIndex == 0 && comboBox2.SelectedIndex == 3)
			//{
			//    //pSOM.InitPhase2(1, 0);	//InitialLearningRate//pSOM.LearningRate()
			//    pSOM.InitPhase2(NumIterations, 0.5 * InitialLearningRate);	//InitialLearningRate//pSOM.LearningRate()
			//}
			else
			{
				timer1.Enabled = false;

				statusBar1.Text = "Iteration: " + _som.IterationCount().ToString("0000") +
					"   Total Error 1: " + totalError.ToString("0.0000") +
					"   Total Error 2: " + totalError2.ToString("0.0000");
			}
		}

		bool Train()
		{
			double[][] trainingSet;

			if (comboBox2.SelectedIndex == 0)
				trainingSet = _randomTrainingSet;
			else if (comboBox2.SelectedIndex == 1)
				trainingSet = _gradualTrainingSet;
			else if (comboBox2.SelectedIndex == 2)
				trainingSet = _predefTrainingSet;
			else
			{
				double[] TraininData = new double[SizeOfInputVector];
				for (int i = 0; i < SizeOfInputVector; i++)
					TraininData[i] = _rnd.NextDouble();

				return _som.Epoch(TraininData);
			}

			int iVector = _rnd.Next(_numTrainingSet);

			return _som.Epoch(trainingSet[iVector]);

			//double[] TraininData;
			//if (comboBox2.SelectedIndex == 0)
			//    TraininData = _randomTrainingSet[iVector];
			//else if (comboBox2.SelectedIndex == 1)
			//    TraininData = _gradualTrainingSet[iVector];
			//else
			//    TraininData = _predefTrainingSet[iVector];
			//return pSOM.Epoch(TraininData);
		}

	}
}
