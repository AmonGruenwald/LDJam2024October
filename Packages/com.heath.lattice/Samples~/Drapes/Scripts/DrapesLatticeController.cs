using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Samples
{
	/// <summary>
	/// An example of controlling a lattice to follow a sine wave.
	/// </summary>
	public class DrapesLatticeController : MonoBehaviour
	{
		[SerializeField] private float _speed;
		[SerializeField] private float _scale;
		[SerializeField] private float _distance;
		private Lattice _lattice;

		private void OnEnable()
		{
			_lattice = GetComponent<Lattice>();
		}

		private void Update()
		{
			for (int i = 0; i < _lattice.Resolution.x; i++)
			{
				for (int j = 0; j < _lattice.Resolution.y; j++)
				{
					for (int k = 0; k < _lattice.Resolution.z; k++)
					{
						// Determine offset amount
						float position = _scale * i;
						float wind = Mathf.Sin(_speed * Time.timeSinceLevelLoad + position);
						float offset = _distance * j * wind;

						// Set handle offset
						_lattice.SetHandleOffset(i, j, k, new Vector3(0, 0, offset));
					}
				}
			}
		}
	}
}
