using System;
using System.Drawing.Imaging;

namespace aiSomColor
{
	/// <summary>
	/// Summary description for SOM.
	/// </summary>

	struct SOMNode
	{
		public double[] Weights;
		public int X, Y, W;

		public SOMNode(int iX, int iY, int WeightsNum)
		{
			X = iX; Y = iY;
			W = 0;
			Weights = new double[WeightsNum];
		}
	}

	public class SOM
	{
		SOMNode[] m_SOM;

		int _cellsAcross, _cellsUp, _numCells;
		int _weightsNum;

		int _maxWin;
		int _winningNode;

		//this is the topological 'radius' of the feature map
		double _dMapRadius;
		//the current width of the winning node's area of influence
		double _dNeighbourhoodRadius;

		//used in the calculation of the neighbourhood width of influence
		double _dTimeConstant;

		//the number of training iterations
		int _iNumIterations;
		//keeps track of what iteration the epoch method has reached
		int _iIterationCount;
		int _iPhase;

		//how much the learning rate is adjusted for nodes within
		//the area of influence
		double _dInfluence;

		double _startLearningRate;
		double _dLearningRate;

		//set true when training is finished
		bool _bDone;
		bool _initialised;

		Random _rnd = new Random();

		public SOM(int CellsUp, int CellsAcross, int WeightsNum)
		{
			_weightsNum = WeightsNum;
			_cellsAcross = CellsAcross;
			_cellsUp = CellsUp;
			_numCells = CellsUp * CellsAcross;

			m_SOM = new SOMNode[_numCells];

			for (int i = 0; i < _numCells; i++)
				m_SOM[i].Weights = new double[WeightsNum];

			_initialised = false;
			_bDone = true;
		}

		public void InitPhase2(int numIterations, double startLearningRate)
		{
			_bDone = true;
			_maxWin = 0;
			_iIterationCount = 0;
			_iNumIterations = numIterations;
			_startLearningRate = startLearningRate;
			_dLearningRate = _startLearningRate;

			_dMapRadius = 0.5 * Math.Sqrt((double)_cellsAcross * _cellsAcross + (double)_cellsUp * _cellsUp);
			//_dMapRadius = 0.5 * Math.Max(_cellsUp, _cellsAcross);
			//_dMapRadius = 0.5;

			_dTimeConstant = _iNumIterations / Math.Log(_dMapRadius);
			_initialised = true;

			_iPhase = 1;
		}

		public void Init(int mode, int numIterations, double startLearningRate)
		{
			InitPhase2(numIterations, startLearningRate);
			//_dMapRadius = 0.5 * Math.Sqrt((double)_cellsAcross * _cellsAcross + (double)_cellsUp * _cellsUp);
			_iPhase = 0;

			double InvP = 1.0 / ((double)_cellsAcross * _cellsUp);
			double InvDiag = 1.0 / ((double)_cellsAcross * _cellsAcross + (double)_cellsUp * _cellsUp);

			//for (int j = 0; j < _cellsUp; j++)
			//{
			//    for (int i = 0; i < _cellsAcross; i++)
			//    {
			//        int cell = j * _cellsAcross + i;
			//        m_SOM[cell].X = i;
			//        m_SOM[cell].Y = j;
			//        m_SOM[cell].W = 0;

			//        for (int w = 0; w < _weightsNum; w++)
			//            m_SOM[cell].Weights[w] = 0.0;
			//    }
			//}
			//return;

			switch (mode)
			{
				case 1:			//Gradual weights
					InvP = 1.0 / (_cellsUp + _cellsAcross);
					for (int j = 0; j < _cellsUp; j++)
					{
						for (int i = 0; i < _cellsAcross; i++)
						{
							int cell = j * _cellsAcross + i;
							m_SOM[cell].X = i;
							m_SOM[cell].Y = j;
							m_SOM[cell].W = 0;

							double ww = (i + j) * InvP;
							for (int w = 0; w < _weightsNum; w++)
								m_SOM[cell].Weights[w] = ww;
						}
					}
					break;

				case 2:			//Ordered 1
					for (int j = 0; j < _cellsUp; j++)
					{
						for (int i = 0; i < _cellsAcross; i++)
						{
							int cell = j * _cellsAcross + i;
							m_SOM[cell].X = i;
							m_SOM[cell].Y = j;
							m_SOM[cell].W = 0;

							double x, y;
							double wr, wg, wb;
							x = _cellsAcross - i;
							y = j;
							wr = x * y * InvP;					//red

							x = i;
							y = j;
							wg = x * y * InvP;					//green

							x = i;
							y = _cellsUp - j;
							wb = x * y * InvP;					//blue

							//if (wr < 0)						wr = 0;
							//else if (wr > 1)					wr = 1;

							//if (wg < 0)						wg = 0;
							//else if (wg > 1)					wg = 1;

							//if (wb < 0)						wb = 0;
							//else if (wb > 1)					wb = 1;

							m_SOM[cell].Weights[0] = wr;
							m_SOM[cell].Weights[1] = wg;
							m_SOM[cell].Weights[2] = wb;

							double ww = i * j * InvP;
							for (int w = 3; w < _weightsNum; w++)
								m_SOM[cell].Weights[w] = ww;
						}
					}
					break;

				case 3:					//Ordered 1 light
					for (int j = 0; j < _cellsUp; j++)
					{
						for (int i = 0; i < _cellsAcross; i++)
						{
							int cell = j * _cellsAcross + i;
							m_SOM[cell].X = i;
							m_SOM[cell].Y = j;
							m_SOM[cell].W = 0;

							double x, y;
							double wr, wg, wb;
							x = _cellsAcross - i;
							y = j;
							wr = (x * x + y * y) * InvDiag;	//red

							x = i;
							y = j;
							wg = (x * x + y * y) * InvDiag;	//green

							x = i;
							y = _cellsUp - j;
							wb = (x * x + y * y) * InvDiag;	//blue

							//if (wr < 0)						wr = 0;
							//else if (wr > 1)					wr = 1;

							//if (wg < 0)						wg = 0;
							//else if (wg > 1)					wg = 1;

							//if (wb < 0)						wb = 0;
							//else if (wb > 1)					wb = 1;

							m_SOM[cell].Weights[0] = wr;
							m_SOM[cell].Weights[1] = wg;
							m_SOM[cell].Weights[2] = wb;

							double ww = i * j * InvP;
							for (int w = 3; w < _weightsNum; w++)
								m_SOM[cell].Weights[w] = ww;
						}
					}
					break;

				case 4:			//Ordered 2
					double theta1 = 90.0 * Math.PI / 180.0;
					double theta2 = 210.0 * Math.PI / 180.0;
					double theta3 = 330.0 * Math.PI / 180.0;

					double H2 = 0.5 * _cellsUp,
							H4 = 0.25 * _cellsUp,
							W2 = 0.5 * _cellsAcross;

					double rX = Math.Cos(theta1) * H4 + W2;
					double rY = Math.Sin(theta1) * H4 + H2;

					double gX = Math.Cos(theta2) * H4 + W2;
					double gY = Math.Sin(theta2) * H4 + H2;

					double bX = Math.Cos(theta3) * H4 + W2;
					double bY = Math.Sin(theta3) * H4 + H2;

					for (int j = 0; j < _cellsUp; j++)
					{
						for (int i = 0; i < _cellsAcross; i++)
						{
							int cell = j * _cellsAcross + i;
							m_SOM[cell].X = i;
							m_SOM[cell].Y = j;
							m_SOM[cell].W = 0;

							double dx, dy, wr, wg, wb;

							dx = Math.Abs(rX - i); dy = Math.Abs(rY - j);
							wr = 2 * dx * dy * InvP;				//red

							dx = Math.Abs(gX - i); dy = Math.Abs(gY - j);
							wg = 2 * dx * dy * InvP;				//green

							dx = Math.Abs(bX - i); dy = Math.Abs(bY - j);
							wb = 2 * dx * dy * InvP;				//blue

							//if (wr < 0)							wr = 0;
							//else if (wr > 1)						wr = 1;

							//if (wg < 0)							wg = 0;
							//else if (wg > 1)						wg = 1;

							//if (wb < 0)							wb = 0;
							//else if (wb > 1)						wb = 1;


							m_SOM[cell].Weights[0] = wr;
							m_SOM[cell].Weights[1] = wg;
							m_SOM[cell].Weights[2] = wb;

							double ww = 0.5 * (i * i + j * j) * InvP;
							for (int w = 3; w < _weightsNum; w++)
								m_SOM[cell].Weights[w] = ww;
						}
					}
					break;

				case 5:			//Ordered 2 light
					theta1 = 90.0 / 180.0 * Math.PI;
					theta2 = 210.0 / 180.0 * Math.PI;
					theta3 = 330.0 / 180.0 * Math.PI;

					H2 = 0.5 * _cellsUp;
					H4 = 0.25 * _cellsUp;
					W2 = 0.5 * _cellsAcross;

					rX = Math.Cos(theta1) * H4 + W2;
					rY = Math.Sin(theta1) * H4 + H2;

					gX = Math.Cos(theta2) * H4 + W2;
					gY = Math.Sin(theta2) * H4 + H2;

					bX = Math.Cos(theta3) * H4 + W2;
					bY = Math.Sin(theta3) * H4 + H2;

					for (int j = 0; j < _cellsUp; j++)
					{
						for (int i = 0; i < _cellsAcross; i++)
						{
							int cell = j * _cellsAcross + i;
							m_SOM[cell].X = i;
							m_SOM[cell].Y = j;
							m_SOM[cell].W = 0;

							double dx, dy, wr, wg, wb;

							dx = Math.Abs(rX - i); dy = Math.Abs(rY - j);
							wr = (dx * dx + dy * dy) * InvP;		//red

							dx = Math.Abs(gX - i); dy = Math.Abs(gY - j);
							wg = (dx * dx + dy * dy) * InvP;		//green

							dx = Math.Abs(bX - i); dy = Math.Abs(bY - j);
							wb = (dx * dx + dy * dy) * InvP;		//blue

							//if (wr < 0)							wr = 0;
							//else if (wr > 1)						wr = 1;

							//if (wg < 0)							wg = 0;
							//else if (wg > 1)						wg = 1;

							//if (wb < 0)							wb = 0;
							//else if (wb > 1)						wb = 1;

							m_SOM[cell].Weights[0] = wr;
							m_SOM[cell].Weights[1] = wg;
							m_SOM[cell].Weights[2] = wb;

							double ww = 0.5 * (i * i + j * j) * InvP;
							for (int w = 3; w < _weightsNum; w++)
								m_SOM[cell].Weights[w] = ww;
						}
					}
					break;
				default:		//Random weights
					for (int j = 0; j < _cellsUp; j++)
						for (int i = 0; i < _cellsAcross; i++)
						{
							int cell = j * _cellsAcross + i;

							m_SOM[cell].X = i;
							m_SOM[cell].Y = j;
							m_SOM[cell].W = 0;

							for (int w = 0; w < _weightsNum; w++)
								m_SOM[cell].Weights[w] = _rnd.NextDouble();
						}
					break;
			}
		}

		int FindBestMatchingNode(double[] vec)
		{
			int winner = -1;
			double LowestDistance = double.MaxValue;

			for (int i = 0; i < _numCells; ++i)
			{
				//	Calculate distance to vec;
				double dist = 0.0;
				for (int w = 0; w < _weightsNum; ++w)
					dist += (vec[w] - m_SOM[i].Weights[w]) * (vec[w] - m_SOM[i].Weights[w]);

				if (dist < LowestDistance)
				{
					LowestDistance = dist;
					winner = i;
				}
			}

			return winner;
		}

		int FindBestMatchingNode2(double[] vec)
		{
			int match_amt = 0, n;
			double LowestDistance = 999999;

			int[] match_list = new int[_numCells];

			for (n = 0; n < _numCells; n++)
			{
				double dist = 0;
				for (int i = 0; i < _weightsNum; ++i)
					dist += (vec[i] - m_SOM[n].Weights[i]) * (vec[i] - m_SOM[n].Weights[i]);

				if (dist < LowestDistance)
				{
					LowestDistance = dist;
					match_list[0] = n;
					match_amt = 1;
				}
				else if (dist == LowestDistance && match_amt < _numCells)
					match_list[match_amt++] = n;
			}

			if (match_amt <= 1)
				return match_list[0];

			return match_list[_rnd.Next() % match_amt];
		}

		public bool Epoch(double[] data)
		{
			//return if the training is complete
			_bDone = _iNumIterations <= 0;
			if (_bDone) return true;

			_initialised = false;
			_iNumIterations--;

			//present the vector to each node and determine the BMU
			_winningNode = FindBestMatchingNode(data);
			m_SOM[_winningNode].W++;
			if (m_SOM[_winningNode].W > _maxWin)
				_maxWin = m_SOM[_winningNode].W;

			//calculate the width of the neighbourhood for this timestep
			_dNeighbourhoodRadius = _dMapRadius * Math.Exp(-(double)_iIterationCount / _dTimeConstant);
			double WidthSq = _dNeighbourhoodRadius * _dNeighbourhoodRadius;
			double InvWidthSq = -0.5 / WidthSq;

			int R2 = (int)Math.Ceiling(_dNeighbourhoodRadius);
			//int R2 = (int)Math.Round(m_dNeighbourhoodRadius);
			//int R2 = (int)Math.Floor(m_dNeighbourhoodRadius);

			int jS = m_SOM[_winningNode].Y - R2;
			int jE = m_SOM[_winningNode].Y + R2;

			int iS = m_SOM[_winningNode].X - R2;
			int iE = m_SOM[_winningNode].X + R2;

			if (jS < 0) jS = 0;
			if (jE > _cellsUp) jE = _cellsUp;

			if (iS < 0) iS = 0;
			if (iE > _cellsAcross) iE = _cellsAcross;

			//Now to adjust the weight vector of the BMU and its neighbours

			//For each node calculate the m_dInfluence (Theta from equation 6 in
			//the tutorial. If it is greater than zero adjust the node's weights
			//accordingly

			for (int j = jS; j < jE; j++)
				for (int i = iS; i < iE; i++)
				{
					//calculate the Euclidean distance (squared) to this node from the BMU
					double DistToNodeSq = ((double)m_SOM[_winningNode].X - i) * ((double)m_SOM[_winningNode].X - i) +
											((double)m_SOM[_winningNode].Y - j) * ((double)m_SOM[_winningNode].Y - j);

					//if within the neighbourhood adjust its weights
					if (DistToNodeSq <= WidthSq)
					{
						//calculate by how much its weights are adjusted
						_dInfluence = _dLearningRate * Math.Exp(DistToNodeSq * InvWidthSq);
						//_dInfluence = _dInfluence / (4 * _dInfluence + 1);

						// Adjust Weights
						int cell = j * _cellsAcross + i;
						for (int w = 0; w < _weightsNum; ++w)
							m_SOM[cell].Weights[w] += _dInfluence * (data[w] - m_SOM[cell].Weights[w]);
					}
				}

			//reduce the learning rate
			_dLearningRate = _startLearningRate * Math.Exp(-(double)_iIterationCount / _iNumIterations);
			_iIterationCount++;

			return false;
		}

		public bool Epoch2(double[] data)
		{
			//return if the training is complete
			_bDone = _iNumIterations <= 0;
			if (_bDone) return true;

			_initialised = false;		//??/!!!!!
			_iNumIterations--;

			//present the vector to each node and determine the BMU
			_winningNode = FindBestMatchingNode(data);
			m_SOM[_winningNode].W++;
			if (m_SOM[_winningNode].W > _maxWin)
				_maxWin = m_SOM[_winningNode].W;

			//calculate the width of the neighbourhood for this timestep
			_dNeighbourhoodRadius = _dMapRadius * Math.Exp(-(double)_iIterationCount / _dTimeConstant);
			double WidthSq = _dNeighbourhoodRadius * _dNeighbourhoodRadius;
			double InvWidthSq = -0.5 / WidthSq;

			//Now to adjust the weight vector of the BMU and its neighbours

			//For each node calculate the m_dInfluence (Theta from equation 6 in
			//the tutorial. If it is greater than zero adjust the node's weights
			//accordingly
			for (int n = 0; n < _numCells; ++n)
			{
				//calculate the Euclidean distance (squared) to this node from the BMU
				double DistToNodeSq = (m_SOM[_winningNode].X - m_SOM[n].X) * (m_SOM[_winningNode].X - m_SOM[n].X) +
										(m_SOM[_winningNode].Y - m_SOM[n].Y) * (m_SOM[_winningNode].Y - m_SOM[n].Y);

				//if within the neighbourhood adjust its weights
				if (DistToNodeSq < WidthSq)
				{
					//calculate by how much its weights are adjusted
					_dInfluence = _dLearningRate * Math.Exp(DistToNodeSq * InvWidthSq);
					//_dInfluence = _dInfluence / (4 * _dInfluence + 1);

					// Adjust Weights
					for (int w = 0; w < _weightsNum; ++w)
						m_SOM[n].Weights[w] += _dInfluence * (data[w] - m_SOM[n].Weights[w]);
				}
			}//next node

			//reduce the learning rate
			_dLearningRate = _startLearningRate * Math.Exp(-(double)_iIterationCount / _iNumIterations);
			++_iIterationCount;

			return false;
		}

		unsafe public void RenderBlock(int xLeft, int xRight, int yTop, int yBottom, int color, BitmapData BMPData)
		{
			int i, j, s;
			int* pix;
			int* pBase;

			s = Math.Abs(BMPData.Stride);
			pBase = (int*)BMPData.Scan0.ToPointer();
			pBase += yTop * (s >> 2) + xLeft;

			for (j = yTop; j < yBottom; j++)
			{
				pix = pBase;
				for (i = xLeft; i < xRight; i++)
					*pix++ = color;

				pBase += (s >> 2);
			}
		}

		public void RenderWeight(BitmapData BMPData)
		{
			double kx = (double)BMPData.Width / _cellsAcross;
			double ky = (double)BMPData.Height / _cellsUp;

			for (int j = 0; j < _cellsUp; j++)
			{
				int yt = (int)Math.Round(j * ky);
				int yb = (int)Math.Round((j + 1) * ky);

				for (int i = 0; i < _cellsAcross; i++)
				{
					int xl = (int)Math.Round(i * kx);
					int xr = (int)Math.Round((i + 1) * kx);
					int color = toRGB(m_SOM[j * _cellsAcross + i].Weights);

					RenderBlock(xl, xr, yt, yb, color, BMPData);
				}
			}
		}

		public int RenderWinner(BitmapData BMPData)
		{
			double kx = (double)BMPData.Width / _cellsAcross;
			double ky = (double)BMPData.Height / _cellsUp;
			int winners = 0;
			for (int j = 0; j < _cellsUp; j++)
			{
				int yt = (int)Math.Round(j * ky);
				int yb = (int)Math.Round((j + 1) * ky);

				for (int i = 0; i < _cellsAcross; i++)
				{
					int xl = (int)Math.Round(i * kx);
					int xr = (int)Math.Round((i + 1) * kx);

					int color = toRGBColored(m_SOM[j * _cellsAcross + i].W / (double)_maxWin);
					if (m_SOM[j * _cellsAcross + i].W > 0)
						winners++;

					RenderBlock(xl, xr, yt, yb, color, BMPData);
				}
			}

			return winners;
		}

		public double RenderError(BitmapData BMPData, int SimWwight, bool bColored = false)
		{
			int i, j;
			double[,] t_map = new double[_cellsAcross, _cellsUp];

			double total = 0.0;
			double max_dist = 0;

			for (j = 0; j < _cellsUp; j++)
				for (i = 0; i < _cellsAcross; i++)
				{
					//int cell = j * _cellsAcross + i;
					int numinave = -1;
					SOMNode center = m_SOM[j * _cellsAcross + i];

					//Total up the distances
					for (int y = -SimWwight; y <= SimWwight; y++)
						for (int x = -SimWwight; x <= SimWwight; x++)
							if ((j + y) >= 0 && (j + y) < _cellsUp &&
								(i + x) >= 0 && (i + x) < _cellsAcross)
							{
								double dist = 0;
								for (int k = 0; k < _weightsNum; ++k)
									//dist += (m_SOM[(j + y) * _cellsAcross + i + x].Weights[k] - m_SOM[cell].Weights[k]) *
									//		  (m_SOM[(j + y) * _cellsAcross + i + x].Weights[k] - m_SOM[cell].Weights[k]);

									dist += (m_SOM[(j + y) * _cellsAcross + i + x].Weights[k] - center.Weights[k]) *
											(m_SOM[(j + y) * _cellsAcross + i + x].Weights[k] - center.Weights[k]);
								total += Math.Sqrt(dist);
								numinave++;
							}

					//-1 is for the center, no cuenta
					total /= (double)(numinave);
					if (total > max_dist)
						max_dist = total;

					//Put all the totals into a buffer for later scaling
					t_map[i, j] = total;
				}

			double kx = (double)BMPData.Width / _cellsAcross;
			double ky = (double)BMPData.Height / _cellsUp;

			total = 0;

			//int color0 = toRGB(0.0, 0.0, 0.0);
			//int color1 = toRGB(1, 0, 0);
			//int color2 = toRGB(1, 1, 0);
			//int color3 = toRGB(0, 1, 0);
			//int color4 = toRGB(0, 1, 1);
			//int color5 = toRGB(1, 1, 1);

			for (j = 0; j < _cellsUp; j++)
			{
				int yt = (int)Math.Round(j * ky);
				int yb = (int)Math.Round((j + 1) * ky);

				for (i = 0; i < _cellsAcross; i++)
				{
					total += t_map[i, j];

					int xl = (int)Math.Round(i * kx);
					int xr = (int)Math.Round((i + 1) * kx);
					int color;
					if (bColored)
						color = toRGBColored(1.0 - t_map[i, j] / max_dist);
					else
						color = toRGB(1.0 - t_map[i, j] / max_dist);

					RenderBlock(xl, xr, yt, yb, color, BMPData);
				}
			}

			return total;
		}

		static public int toRGB(double r, double g, double b)
		{
			return ((int)(b * 255)) | ((int)(g * 255) << 8) | ((int)(r * 255) << 16);
		}

		static public int toRGB(double[] w)
		{
			return ((int)(w[2] * 255)) | ((int)(w[1] * 255) << 8) | ((int)(w[0] * 255) << 16);
		}

		static public int toRGB(double v)
		{
			return ((int)(v * 255)) | ((int)(v * 255) << 8) | ((int)(v * 255) << 16);
		}

		static public int toRGBColored(double v)
		{
			//const double treshold0 = 0.0;
			const double treshold1 = 0.2;
			const double treshold2 = 0.4;
			const double treshold3 = 0.6;
			const double treshold4 = 0.8;
			const double treshold5 = 1.0;

			int color0 = toRGB(0.0, 0.0, 0.0);
			int color1 = toRGB(1, 0, 0);
			int color2 = toRGB(1, 1, 0);
			int color3 = toRGB(0, 1, 0);
			int color4 = toRGB(0, 1, 1);
			int color5 = toRGB(1, 1, 1);

			int r, g, b;
			int c0, c1;
			double k0, k1;

			if (v <= treshold1)
			{
				k0 = v / treshold1;
				k1 = 1.0 - k0;
				c0 = color0;
				c1 = color1;
			}
			else if (v <= treshold2)
			{
				k0 = (v - treshold1) / (treshold2 - treshold1);
				k1 = 1.0 - k0;

				c0 = color1;
				c1 = color2;
			}
			else if (v <= treshold3)
			{
				k0 = (v - treshold2) / (treshold3 - treshold2);
				k1 = 1.0 - k0;

				c0 = color2;
				c1 = color3;
			}
			else if (v <= treshold4)
			{
				k0 = (v - treshold3) / (treshold4 - treshold3);
				k1 = 1.0 - k0;

				c0 = color3;
				c1 = color4;
			}
			else //if (v <= treshold5)
			{
				k0 = (v - treshold4) / (treshold5 - treshold4);
				k1 = 1.0 - k0;

				c0 = color4;
				c1 = color5;
			}

			int b0 = c0 & 0xFF;
			int g0 = (c0 >> 8) & 0xFF;
			int r0 = (c0 >> 16) & 0xFF;

			int b1 = c1 & 0xFF;
			int g1 = (c1 >> 8) & 0xFF;
			int r1 = (c1 >> 16) & 0xFF;

			r = (int)(r0 * k1 + r1 * k0);
			g = (int)(g0 * k1 + g1 * k0);
			b = (int)(b0 * k1 + b1 * k0);

			return b | (g << 8) | (r << 16);
		}


		static public int toRGBColored(double v, int color0, int color1, int color2, int color3, int color4, int color5)
		{
			//const double treshold0 = 0.0;
			const double treshold1 = 0.20;
			const double treshold2 = 0.40;
			const double treshold3 = 0.60;
			const double treshold4 = 0.80;
			const double treshold5 = 1.0;

			int r, g, b;
			int c0, c1;
			double k0, k1;

			if (v <= treshold1)
			{
				k0 = v / treshold1;
				k1 = 1.0 - k0;

				c0 = color0;
				c1 = color1;
			}
			else if (v <= treshold2)
			{
				k0 = (v - treshold1) / (treshold2 - treshold1);
				k1 = 1.0 - k0;

				c0 = color1;
				c1 = color2;
			}
			else if (v <= treshold3)
			{
				k0 = (v - treshold2) / (treshold3 - treshold2);
				k1 = 1.0 - k0;

				c0 = color2;
				c1 = color3;
			}
			else if (v <= treshold4)
			{
				k0 = (v - treshold3) / (treshold4 - treshold3);
				k1 = 1.0 - k0;

				c0 = color3;
				c1 = color4;
			}
			else //if (v <= treshold5)
			{
				k0 = (v - treshold4) / (treshold5 - treshold4);
				k1 = 1.0 - k0;

				c0 = color4;
				c1 = color5;
			}

			int b0 = c0 & 0xFF;
			int g0 = (c0 >> 8) & 0xFF;
			int r0 = (c0 >> 16) & 0xFF;

			int b1 = c1 & 0xFF;
			int g1 = (c1 >> 8) & 0xFF;
			int r1 = (c1 >> 16) & 0xFF;

			r = (int)(r0 * k1 + r1 * k0);
			g = (int)(g0 * k1 + g1 * k0);
			b = (int)(b0 * k1 + b1 * k0);

			return b | (g << 8) | (r << 16);
		}

		static public int toRGBColored(double v, int color0, int color1, int color2, int color3)
		{
			//const double treshold0 = 0.0;
			const double treshold1 = 0.333333;
			const double treshold2 = 0.666666;
			const double treshold3 = 1.0;

			int r, g, b;
			int c0, c1;
			double k0, k1;

			if (v <= treshold1)
			{
				k0 = v / treshold1;
				k1 = 1.0 - k0;

				c0 = color0;
				c1 = color1;
			}
			else if (v <= treshold2)
			{
				k0 = (v - treshold1) / (treshold2 - treshold1);
				k1 = 1.0 - k0;

				c0 = color1;
				c1 = color2;
			}
			else //if (v <= treshold3)
			{
				k0 = (v - treshold2) / (treshold3 - treshold2);
				k1 = 1.0 - k0;

				c0 = color2;
				c1 = color3;
			}

			int b0 = c0 & 0xFF;
			int g0 = (c0 >> 8) & 0xFF;
			int r0 = (c0 >> 16) & 0xFF;

			int b1 = c1 & 0xFF;
			int g1 = (c1 >> 8) & 0xFF;
			int r1 = (c1 >> 16) & 0xFF;

			r = (int)(r0 * k1 + r1 * k0);
			g = (int)(g0 * k1 + g1 * k0);
			b = (int)(b0 * k1 + b1 * k0);

			return b | (g << 8) | (r << 16);
		}

		public double LearningRate()
		{
			return _dLearningRate;
		}

		public double NeighbourhoodRadius()
		{
			return _dNeighbourhoodRadius;
		}

		public double Influence()
		{
			return _dInfluence;
		}

		public int IterationCount()
		{
			return _iIterationCount;
		}

		public int Phase()
		{
			return _iPhase;
		}

		public bool FinishedTraining()
		{
			return _bDone;
		}

		public bool Initialised()
		{
			return _initialised;
		}

		/*
		bool Train()
		{
			if (!m_bDone)
			{
				double[] TraininData = new double[m_WeightsNum];
				for (int i = 0; i < m_WeightsNum; i++)
					TraininData[i] = _rnd.NextDouble();

				if (!Epoch(TraininData))
					return false;

				//int ThisVector = _rnd.Next(_numTrainingSet);
				//if (!Epoch(m_TrainingSet[ThisVector]))
				//	return false;
			}
			return true;
		}
		*/
	}
}
