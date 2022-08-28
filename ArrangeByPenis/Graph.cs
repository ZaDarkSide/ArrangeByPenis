using System;
using System.Collections.Generic;
using System.Text;

namespace ArrangeByPenis
{
	class Graph
	{
		class Node
		{
			public double sample;
			public double x;
			public double y;

			public Node(double s, double _x, double _y)
			{
				sample = s;
				x = _x;
				y = _y;
			}
		}

		LinkedList<Node> points;
		double end;

		public double End
		{
			get { return end; }
		}

		public Graph()
		{
			points = new LinkedList<Node>();

			points.AddLast(new Node(0.0, 0.75, 0.78));
			points.AddLast(new Node(1.0, 0.75, 0.86));

			points.AddLast(new Node(3.0, 0.60, 0.9));
			points.AddLast(new Node(4.0, 0.56, 0.83));
			points.AddLast(new Node(5.5, 0.59, 0.64));
			points.AddLast(new Node(6.5, 0.69, 0.53));

			points.AddLast(new Node(8.5, 0.69, 0.22));
			points.AddLast(new Node(10.5, 0.72, 0.08));
			points.AddLast(new Node(11, 0.75, 0.04));

			points.AddLast(new Node(11.5, 0.78, 0.08));
			points.AddLast(new Node(13.5, 0.81, 0.22));
			points.AddLast(new Node(15.5, 0.81, 0.53));

			points.AddLast(new Node(16.5, 0.91, 0.64));
			points.AddLast(new Node(18.0, 0.94, 0.83));
			points.AddLast(new Node(19.0, 0.91, 0.9));
			points.AddLast(new Node(21.0, 0.75, 0.86));

			points.AddLast(new Node(21.0, 0.69, 0.32));
			points.AddLast(new Node(22.0, 0.75, 0.29));
			points.AddLast(new Node(23.0, 0.81, 0.32));

			end = 23.0;
		}

		public Point getPoint(double v, int width, int height)
		{
			Node lastNode = null;
			
			foreach (Node n in points)
			{
				if (n.sample == v)
					return new Point((int)(n.x * width), (int)(n.y * height));
				else if (n.sample > v)
				{
					double f = (v - lastNode.sample) / (n.sample - lastNode.sample);
					double x = lastNode.x + f * (n.x - lastNode.x);
					double y = lastNode.y + f * (n.y - lastNode.y);

					return new Point((int)(x * width), (int)(y * height));
				}

				lastNode = n;
			}
			throw new Exception("unexpected argument on getPoint: " + v);
		}
	}
}
