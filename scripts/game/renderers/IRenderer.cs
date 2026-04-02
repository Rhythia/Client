using System.Collections.Generic;

public interface IRenderer<T>
{
	void Render(double time, double dt, IList<T> objects);
}
