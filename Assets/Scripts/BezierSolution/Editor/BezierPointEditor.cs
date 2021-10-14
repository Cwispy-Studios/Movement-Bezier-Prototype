using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BezierSolution.Extras
{
	[CustomEditor( typeof( BezierPoint ) )]
	[CanEditMultipleObjects]
	public class BezierPointEditor : Editor
	{
		private class SplineHolder
		{
			public BezierSpline spline;
			public BezierPoint[] points;

			public SplineHolder( BezierSpline spline, BezierPoint[] points )
			{
				this.spline = spline;
				this.points = points;
			}

			public void SortPoints( bool forwards )
			{
				if( forwards )
					System.Array.Sort( points, CompareForwards );
				else
					System.Array.Sort( points, CompareBackwards );
			}

			private int CompareForwards( BezierPoint x, BezierPoint y )
			{
				return x.Internal_Index.CompareTo( y.Internal_Index );
			}

			private int CompareBackwards( BezierPoint x, BezierPoint y )
			{
				return y.Internal_Index.CompareTo( x.Internal_Index );
			}
		}

		private const float CONTROL_POINTS_MINIMUM_SAFE_DISTANCE_SQR = 0.05f * 0.05f;

		private static readonly Color RESET_POINT_BUTTON_COLOR = new Color( 1f, 1f, 0.65f );
		private static readonly Color REMOVE_POINT_BUTTON_COLOR = new Color( 1f, 0.65f, 0.65f );
		private static readonly GUIContent MULTI_EDIT_TIP = new GUIContent( "Tip: Hold Shift to affect all points' Transforms" );
		private static readonly GUIContent EXTRA_DATA_SET_AS_CAMERA = new GUIContent( "C", "Set as Scene camera's current rotation" );
		private static readonly GUIContent EXTRA_DATA_VIEW_AS_FRUSTUM = new GUIContent( "V", "Visualize data as camera frustum in Scene" );

		private SplineHolder[] selection;
		private BezierSpline[] allSplines;
		private BezierPoint[] allPoints;
		private int pointCount;

		private Quaternion[] precedingPointRotations;
		private Quaternion[] followingPointRotations;
		private bool controlPointRotationsInitialized;

		// Having two variables allow us to show frustum gizmos only when a point is selected
		// and not lose the original value when OnDisable is called
		private static bool m_visualizeExtraDataAsFrustum;
		public static bool VisualizeExtraDataAsFrustum { get; private set; }

		private Tool previousTool = Tool.None;

		private void OnEnable()
		{
			Object[] points = targets;
			pointCount = points.Length;
			allPoints = new BezierPoint[pointCount];

			precedingPointRotations = new Quaternion[pointCount];
			followingPointRotations = new Quaternion[pointCount];
			controlPointRotationsInitialized = false;

			if( pointCount == 1 )
			{
				BezierPoint point = (BezierPoint) points[0];

				selection = new SplineHolder[1] { new SplineHolder( point.GetComponentInParent<BezierSpline>(), new BezierPoint[1] { point } ) };
				allSplines = selection[0].spline ? new BezierSpline[1] { selection[0].spline } : new BezierSpline[0];
				allPoints[0] = point;

			}
			else
			{
				Dictionary<BezierSpline, List<BezierPoint>> lookupTable = new Dictionary<BezierSpline, List<BezierPoint>>( pointCount );
				List<BezierPoint> nullSplinePoints = null;

				for( int i = 0; i < pointCount; i++ )
				{
					BezierPoint point = (BezierPoint) points[i];
					BezierSpline spline = point.GetComponentInParent<BezierSpline>();
					if( !spline )
					{
						spline = null;

						if( nullSplinePoints == null )
							nullSplinePoints = new List<BezierPoint>( pointCount );

						nullSplinePoints.Add( point );
					}
					else
					{
						List<BezierPoint> _points;
						if( !lookupTable.TryGetValue( spline, out _points ) )
						{
							_points = new List<BezierPoint>( pointCount );
							lookupTable[spline] = _points;
						}

						_points.Add( point );
					}

					allPoints[i] = point;
				}

				int index;
				if( nullSplinePoints != null )
				{
					index = 1;
					selection = new SplineHolder[lookupTable.Count + 1];
					selection[0] = new SplineHolder( null, nullSplinePoints.ToArray() );
				}
				else
				{
					index = 0;
					selection = new SplineHolder[lookupTable.Count];
				}

				int index2 = 0;
				allSplines = new BezierSpline[lookupTable.Count];

				foreach( var element in lookupTable )
				{
					selection[index++] = new SplineHolder( element.Key, element.Value.ToArray() );
					allSplines[index2++] = element.Key;
				}
			}

			for( int i = 0; i < selection.Length; i++ )
			{
				selection[i].SortPoints( true );

				if( selection[i].spline )
					selection[i].spline.Refresh();
			}

			VisualizeExtraDataAsFrustum = m_visualizeExtraDataAsFrustum;
			Tools.hidden = true;

			Undo.undoRedoPerformed -= OnUndoRedo;
			Undo.undoRedoPerformed += OnUndoRedo;
		}

		private void OnDisable()
		{
			VisualizeExtraDataAsFrustum = false;

			Tools.hidden = false;
			Undo.undoRedoPerformed -= OnUndoRedo;
		}

		private void OnSceneGUI()
		{
			BezierPoint point = (BezierPoint) target;
			if( !point )
				return;

			if( CheckCommands() )
				return;

			// OnSceneGUI is called separately for each selected point,
			// make sure that the spline is drawn only once, not multiple times
			if( point == allPoints[0] )
			{
				for( int i = 0; i < selection.Length; i++ )
				{
					BezierSpline spline = selection[i].spline;

					if( spline )
					{
						BezierPoint[] points = selection[i].points;
						BezierUtils.DrawSplineDetailed( spline );
						for( int j = 0, k = 0; j < spline.Count; j++ )
						{
							bool isSelected = spline[j] == points[k];
							if( isSelected && k < points.Length - 1 )
								k++;

							if( !isSelected )
								BezierUtils.DrawBezierPoint( spline[j], j + 1, false );
						}
					}
				}

				if( allPoints.Length > 1 )
				{
					Handles.BeginGUI();
					GUIStyle style = "PreOverlayLabel"; // Taken from: https://github.com/Unity-Technologies/UnityCsReference/blob/f78f4093c8a2b45949a847cdc704cf209dcf2f36/Editor/Mono/EditorGUI.cs#L629
					EditorGUI.DropShadowLabel( new Rect( new Vector2( 0f, 0f ), style.CalcSize( MULTI_EDIT_TIP ) ), MULTI_EDIT_TIP, style );
					Handles.EndGUI();
				}
			}

			// When Control key is pressed, BezierPoint gizmos should be drawn on top of Transform handles in order to allow selecting/deselecting points
			// If Alt key is pressed, Transform handles aren't drawn at all, so BezierPoint gizmos can be drawn immediately
			Event e = Event.current;
			if( e.alt || !e.control )
				BezierUtils.DrawBezierPoint( point, point.Internal_Index + 1, true );

			// Camera rotates with Alt key, don't interfere
			if( e.alt )
				return;

			int pointIndex = -1;
			for( int i = 0; i < allPoints.Length; i++ )
			{
				if( allPoints[i] == point )
				{
					pointIndex = i;
					break;
				}
			}

			Tool tool = Tools.current;
			if( previousTool != tool )
			{
				controlPointRotationsInitialized = false;
				previousTool = tool;
			}

			// Draw Transform handles for control points
			switch( Tools.current )
			{
				case Tool.Move:
					if( !controlPointRotationsInitialized )
					{
						for( int i = 0; i < allPoints.Length; i++ )
						{
							BezierPoint p = allPoints[i];

							precedingPointRotations[i] = Quaternion.LookRotation( p.precedingControlPointPosition - p.position );
							followingPointRotations[i] = Quaternion.LookRotation( p.followingControlPointPosition - p.position );
						}

						controlPointRotationsInitialized = true;
					}

					// No need to show gizmos for control points in Autoconstruct mode
					Vector3 position;
					if( !point.Internal_Spline || point.Internal_Spline.Internal_AutoConstructMode == SplineAutoConstructMode.None )
					{
						EditorGUI.BeginChangeCheck();
						position = Handles.PositionHandle( point.precedingControlPointPosition, Tools.pivotRotation == PivotRotation.Local ? precedingPointRotations[pointIndex] : Quaternion.identity );
						if( EditorGUI.EndChangeCheck() )
						{
							Undo.RecordObject( point, "Move Control Point" );

              point.precedingControlPointPosition = position;
						}

						EditorGUI.BeginChangeCheck();
						position = Handles.PositionHandle( point.followingControlPointPosition, Tools.pivotRotation == PivotRotation.Local ? followingPointRotations[pointIndex] : Quaternion.identity );
						if( EditorGUI.EndChangeCheck() )
						{
							Undo.RecordObject( point, "Move Control Point" );

              point.followingControlPointPosition = position;
						}
					}

					EditorGUI.BeginChangeCheck();
					position = Handles.PositionHandle( point.position, Tools.pivotRotation == PivotRotation.Local ? point.rotation : Quaternion.identity );
					if( EditorGUI.EndChangeCheck() )
					{
						if( !e.shift )
						{
							Undo.RecordObject( point, "Move Point" );
							Undo.RecordObject( point.transform, "Move Point" );

              point.position = position;
						}
						else
						{
							Vector3 delta = position - point.position;

							for( int i = 0; i < allPoints.Length; i++ )
							{
								Undo.RecordObject( allPoints[i], "Move Point" );
								Undo.RecordObject( allPoints[i].transform, "Move Point" );

                allPoints[i].position += delta;
							}
						}
					}

					break;
				case Tool.Rotate:
					Quaternion handleRotation;
					if( Tools.pivotRotation == PivotRotation.Local )
					{
						handleRotation = point.rotation;
						controlPointRotationsInitialized = false;
					}
					else
					{
						if( !controlPointRotationsInitialized )
						{
							for( int i = 0; i < allPoints.Length; i++ )
								precedingPointRotations[i] = Quaternion.identity;

							controlPointRotationsInitialized = true;
						}

						handleRotation = precedingPointRotations[pointIndex];
					}

					EditorGUI.BeginChangeCheck();
					Quaternion rotation = Handles.RotationHandle( handleRotation, point.position );
					if( EditorGUI.EndChangeCheck() )
					{
						float angle;
						Vector3 axis;
						( Quaternion.Inverse( handleRotation ) * rotation ).ToAngleAxis( out angle, out axis );
						axis = handleRotation * axis;

						if( !e.shift )
						{
							Undo.RecordObject( point.transform, "Rotate Point" );

              Vector3 localAxis = point.transform.InverseTransformDirection( axis );
							point.localRotation *= Quaternion.AngleAxis( angle, localAxis );
						}
						else
						{
							for( int i = 0; i < allPoints.Length; i++ )
							{
								Undo.RecordObject( allPoints[i].transform, "Rotate Point" );

                Vector3 localAxis = allPoints[i].transform.InverseTransformDirection( axis );
								allPoints[i].localRotation *= Quaternion.AngleAxis( angle, localAxis );
							}
						}

						if( Tools.pivotRotation == PivotRotation.Global )
							precedingPointRotations[pointIndex] = rotation;
					}

					break;
				case Tool.Scale:
					EditorGUI.BeginChangeCheck();
					Vector3 scale = Handles.ScaleHandle( point.localScale, point.position, point.rotation, HandleUtility.GetHandleSize( point.position ) );
					if( EditorGUI.EndChangeCheck() )
					{
						if( !e.shift )
						{
							Undo.RecordObject( point.transform, "Scale Point" );

              point.localScale = scale;
						}
						else
						{
							Vector3 delta = new Vector3( 1f, 1f, 1f );
							Vector3 prevScale = point.localScale;
							if( prevScale.x != 0f )
								delta.x = scale.x / prevScale.x;
							if( prevScale.y != 0f )
								delta.y = scale.y / prevScale.y;
							if( prevScale.z != 0f )
								delta.z = scale.z / prevScale.z;

							for( int i = 0; i < allPoints.Length; i++ )
							{
								Undo.RecordObject( allPoints[i].transform, "Scale Point" );

                prevScale = allPoints[i].localScale;
								prevScale.Scale( delta );
								allPoints[i].localScale = prevScale;
							}
						}
					}

					break;
			}

			if( e.control )
				BezierUtils.DrawBezierPoint( point, point.Internal_Index + 1, true );
		}

		public override void OnInspectorGUI()
		{
			if( CheckCommands() )
				GUIUtility.ExitGUI();

			BezierUtils.DrawSplineInspectorGUI( allSplines );

			EditorGUILayout.Space();
			BezierUtils.DrawSeparator();

      EditorGUILayout.LabelField("Edit point", EditorStyles.boldLabel);

      GUILayout.BeginHorizontal();

			if( GUILayout.Button( "<-", GUILayout.Width( 45 ) ) )
			{
				Object[] newSelection = new Object[pointCount];
				for( int i = 0, index = 0; i < selection.Length; i++ )
				{
					BezierSpline spline = selection[i].spline;
					BezierPoint[] points = selection[i].points;

					if( spline )
					{
						for( int j = 0; j < points.Length; j++ )
						{
							int prevIndex = points[j].Internal_Index - 1;
							if( prevIndex < 0 )
								prevIndex = spline.Count - 1;

							newSelection[index++] = spline[prevIndex].gameObject;
						}
					}
					else
					{
						for( int j = 0; j < points.Length; j++ )
							newSelection[index++] = points[j].gameObject;
					}
				}

				Selection.objects = newSelection;
				GUIUtility.ExitGUI();
			}

			string pointIndex = ( pointCount == 1 && selection[0].spline ) ? ( allPoints[0].Internal_Index + 1 ).ToString() : "-";
			string splineLength = ( selection.Length == 1 && selection[0].spline ) ? selection[0].spline.Count.ToString() : "-";
			GUILayout.Box( "Selected Point: " + pointIndex + " / " + splineLength, GUILayout.ExpandWidth( true ) );

			if( GUILayout.Button( "->", GUILayout.Width( 45 ) ) )
			{
				Object[] newSelection = new Object[pointCount];
				for( int i = 0, index = 0; i < selection.Length; i++ )
				{
					BezierSpline spline = selection[i].spline;
					BezierPoint[] points = selection[i].points;

					if( spline )
					{
						for( int j = 0; j < points.Length; j++ )
						{
							int nextIndex = points[j].Internal_Index + 1;
							if( nextIndex >= spline.Count )
								nextIndex = 0;

							newSelection[index++] = spline[nextIndex].gameObject;
						}
					}
					else
					{
						for( int j = 0; j < points.Length; j++ )
							newSelection[index++] = points[j].gameObject;
					}
				}

				Selection.objects = newSelection;
				GUIUtility.ExitGUI();
			}

			GUILayout.EndHorizontal();

      EditorGUILayout.Space();

      if ( GUILayout.Button( "Decrement Point's Index" ) )
			{
				Undo.IncrementCurrentGroup();

				for( int i = 0; i < selection.Length; i++ )
				{
					BezierSpline spline = selection[i].spline;
					if( spline )
					{
						selection[i].SortPoints( true );

						BezierPoint[] points = selection[i].points;
						int[] newIndices = new int[points.Length];
						for( int j = 0; j < points.Length; j++ )
						{
							int index = points[j].Internal_Index;
							int newIndex = index - 1;
							if( newIndex < 0 )
								newIndex = spline.Count - 1;

							newIndices[j] = newIndex;
						}

						for( int j = 0; j < points.Length; j++ )
						{
							Undo.RegisterCompleteObjectUndo( points[j].transform.parent, "Change point index" );

							spline.Internal_MovePoint( points[j].Internal_Index, newIndices[j], "Change point index" );
						}

						selection[i].SortPoints( true );
					}
				}

				SceneView.RepaintAll();
			}

			if( GUILayout.Button( "Increment Point's Index" ) )
			{
				Undo.IncrementCurrentGroup();

				for( int i = 0; i < selection.Length; i++ )
				{
					BezierSpline spline = selection[i].spline;
					if( spline )
					{
						selection[i].SortPoints( false );

						BezierPoint[] points = selection[i].points;
						int[] newIndices = new int[points.Length];
						for( int j = 0; j < points.Length; j++ )
						{
							int index = points[j].Internal_Index;
							int newIndex = index + 1;
							if( newIndex >= spline.Count )
								newIndex = 0;

							newIndices[j] = newIndex;
						}

						for( int j = 0; j < points.Length; j++ )
						{
							Undo.RegisterCompleteObjectUndo( points[j].transform.parent, "Change point index" );

              spline.Internal_MovePoint( points[j].Internal_Index, newIndices[j], "Change point index" );
						}

						selection[i].SortPoints( true );
					}
				}

				SceneView.RepaintAll();
			}

      EditorGUILayout.Space();

      if ( GUILayout.Button( "Insert Point Before" ) )
				InsertNewPoints( false );

			if( GUILayout.Button( "Insert Point After" ) )
				InsertNewPoints( true );

      EditorGUILayout.Space();

      if ( GUILayout.Button( "Duplicate Point" ) )
				DuplicateSelectedPoints();

			EditorGUILayout.Space();

      bool hasMultipleDifferentValues = false;
			BezierPoint.HandleMode handleMode = allPoints[0].handleMode;
			for( int i = 1; i < allPoints.Length; i++ )
			{
				if( allPoints[i].handleMode != handleMode )
				{
					hasMultipleDifferentValues = true;
					break;
				}
			}

      BezierSpline selectedSpline = selection[0].spline;

      Color originalGUIColour = GUI.color;

      if (!selectedSpline.movingPointAlongSpline)
      {
        GUI.color = Color.yellow;

        if (GUILayout.Button("Insert Point Along Spline"))
        {
          CreateTempPointAlongSpline();
        }

        GUI.color = originalGUIColour;
      }

      ///////////////////////////////
      //// MOVING POINT ALONG SPLINE
      else
      {
        // Check if temp point still exists
        if (selectedSpline && selectedSpline.tempPoint)
        {
          BezierUtils.DrawSeparator();

          // Normalised T value
          selectedSpline.FindNearestPointOnXZPlane(selectedSpline.tempPoint.transform.position, out float normalisedT);

          EditorGUI.BeginChangeCheck();

          normalisedT = EditorGUILayout.Slider("Normalised T", normalisedT, 0, 1f);

          // Change found
          if (EditorGUI.EndChangeCheck())
          {
            Undo.RecordObject(selectedSpline.tempPoint.transform, "Change Temp Point Position");

            selectedSpline.tempPoint.transform.position = selectedSpline.GetPoint(normalisedT);
          }

          // Confirm and cancel buttons
          GUILayout.BeginHorizontal();

          GUI.color = Color.cyan;

          if (GUILayout.Button("Insert Point"))
          {
            Undo.IncrementCurrentGroup();

            Undo.RegisterCompleteObjectUndo(selectedSpline, "Insert Point Along Spline");

            // Find indices between temp point
            var indices = selectedSpline.GetNearestPointIndicesTo(normalisedT);

            int newIndex = indices.index2;

            // Finds the control points to align with the existing spline
            Vector3 precedingControlPointPosition = selectedSpline.GetPoint(normalisedT - 0.01f);
            //Vector3 followingControlPointPosition = selectedSpline.GetPoint(normalisedT + 0.01f);

            BezierPoint newPoint = selectedSpline.InsertNewPointAt(newIndex);
            newPoint.position = selectedSpline.tempPoint.transform.position;

            // Sets the control points
            newPoint.handleMode = BezierPoint.HandleMode.Mirrored;
            newPoint.precedingControlPointPosition = precedingControlPointPosition;
            //newPoint.followingControlPointPosition = followingControlPointPosition;

            // Lerps the camera behaviour
            CameraBehaviour cameraBehaviour = newPoint.GetComponent<CameraBehaviour>();
            cameraBehaviour.distance = Mathf.Lerp(selectedSpline[indices.index1].cameraBehaviour.distance, selectedSpline[indices.index2 + 1].cameraBehaviour.distance, indices.t);
            cameraBehaviour.height = Mathf.Lerp(selectedSpline[indices.index1].cameraBehaviour.height, selectedSpline[indices.index2 + 1].cameraBehaviour.height, indices.t);

            selectedSpline.movingPointAlongSpline = false;

            Undo.DestroyObjectImmediate(selectedSpline.tempPoint);

            Undo.RegisterCreatedObjectUndo(newPoint.gameObject, "Insert Point Along Spline");
            if (newPoint.transform.parent)
              Undo.RegisterCompleteObjectUndo(newPoint.transform.parent, "Insert Point Along Spline");
          }

          GUI.color = Color.red;

          if (GUILayout.Button("Cancel"))
          {
            Undo.IncrementCurrentGroup();

            Undo.RegisterCompleteObjectUndo(selectedSpline, "Cancel Insert Point");

            Undo.DestroyObjectImmediate(selectedSpline.tempPoint);

            selectedSpline.movingPointAlongSpline = false;
            selectedSpline.tempPoint = null;
          }

          GUI.color = originalGUIColour;

          GUILayout.EndHorizontal();

          EditorGUILayout.Space();

          BezierUtils.DrawSeparator();
        }

        // Object was destroyed in scene, remove references to it and disable moving
        else
        {
          if (selectedSpline)
          {
            selectedSpline.movingPointAlongSpline = false;
            selectedSpline.tempPoint = null;
          }
        }
      }

      //// END
      /////////

      EditorGUILayout.Space();

      EditorGUILayout.LabelField("Edit control points and position", EditorStyles.boldLabel);

      EditorGUILayout.Space();

      if (allPoints[0].Internal_Spline.splineType != SplineType.Wall)
      {
        int index = allPoints[0].Internal_Index;

        // Displays the angle to the previous point, but check if the point has a previous point
        if (index > 0)
        {
          GUI.enabled = false;

          BezierPoint previousPoint = allPoints[0].Internal_Spline[index - 1];

          Vector2 selectedPointPos2d = new Vector2(allPoints[0].position.x, allPoints[0].position.z);
          Vector2 previousPointPos2d = new Vector2(previousPoint.position.x, previousPoint.position.z);

          float distance2D = Vector2.Distance(selectedPointPos2d, previousPointPos2d);
          float distanceHeight = Mathf.Abs(allPoints[0].position.y - previousPoint.position.y);

          float angleToPreviousPoint = Mathf.Atan(distanceHeight / distance2D) * Mathf.Rad2Deg;

          EditorGUILayout.FloatField("Angle to previous point", angleToPreviousPoint);

          GUI.enabled = true;
        }

        // Displays the angle to the next point, but check if the point has a next point
        if (index < allPoints[0].Internal_Spline.Count - 1)
        {
          GUI.enabled = false;

          BezierPoint nextPoint = allPoints[0].Internal_Spline[index + 1];

          Vector2 selectedPointPos2d = new Vector2(allPoints[0].position.x, allPoints[0].position.z);
          Vector2 previousPointPos2d = new Vector2(nextPoint.position.x, nextPoint.position.z);

          float distance2D = Vector2.Distance(selectedPointPos2d, previousPointPos2d);
          float distanceHeight = Mathf.Abs(allPoints[0].position.y - nextPoint.position.y);

          float angleToNextPoint = Mathf.Atan(distanceHeight / distance2D) * Mathf.Rad2Deg;

          EditorGUILayout.FloatField("Angle to next point", angleToNextPoint);

          GUI.enabled = true;
        }
      }

      else
      {

      }

      EditorGUILayout.Space();

      EditorGUI.showMixedValue = hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();
			handleMode = (BezierPoint.HandleMode) EditorGUILayout.EnumPopup( "Handle Mode", handleMode );
			if( EditorGUI.EndChangeCheck() )
			{
				for( int i = 0; i < allPoints.Length; i++ )
				{
					Undo.RecordObject( allPoints[i], "Change Point Handle Mode" );

					allPoints[i].handleMode = handleMode;
				}

				SceneView.RepaintAll();
			}

			EditorGUILayout.Space();

      /////////////////////
      //// CUSTOM BUTTON
      hasMultipleDifferentValues = false;
      Vector3 position = allPoints[0].position;
      for (int i = 1; i < allPoints.Length; i++)
      {
        if (allPoints[i].position != position)
        {
          hasMultipleDifferentValues = true;
          break;
        }
      }

      EditorGUI.showMixedValue = hasMultipleDifferentValues;
      EditorGUI.BeginChangeCheck();
      position = EditorGUILayout.Vector3Field("Global Position", position);
      if (EditorGUI.EndChangeCheck())
      {
        for (int i = 0; i < allPoints.Length; i++)
        {
          Undo.RecordObject(allPoints[i], "Change Point Global Position");
          Undo.RecordObject(allPoints[i].transform, "Change Point Global Position");

          allPoints[i].position = position;
        }

        SceneView.RepaintAll();
      }
      //// END OF CUSTOM BUTTON
      //////////////////////////

      hasMultipleDifferentValues = false;
			position = allPoints[0].precedingControlPointLocalPosition;
			for( int i = 1; i < allPoints.Length; i++ )
			{
				if( allPoints[i].precedingControlPointLocalPosition != position )
				{
					hasMultipleDifferentValues = true;
					break;
				}
			}

			EditorGUI.showMixedValue = hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();
			position = EditorGUILayout.Vector3Field( "Preceding Control Point Local Position", position );
			if( EditorGUI.EndChangeCheck() )
			{
				for( int i = 0; i < allPoints.Length; i++ )
				{
					Undo.RecordObject( allPoints[i], "Change Point Position" );

					allPoints[i].precedingControlPointLocalPosition = position;
				}

				SceneView.RepaintAll();
			}

			hasMultipleDifferentValues = false;
			position = allPoints[0].followingControlPointLocalPosition;
			for( int i = 1; i < allPoints.Length; i++ )
			{
				if( allPoints[i].followingControlPointLocalPosition != position )
				{
					hasMultipleDifferentValues = true;
					break;
				}
			}

			EditorGUI.showMixedValue = hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();
			position = EditorGUILayout.Vector3Field( "Following Control Point Local Position", position );
			if( EditorGUI.EndChangeCheck() )
			{
				for( int i = 0; i < allPoints.Length; i++ )
				{
					Undo.RecordObject( allPoints[i], "Change Point Position" );

					allPoints[i].followingControlPointLocalPosition = position;
				}

				SceneView.RepaintAll();
			}

			bool showControlPointDistanceWarning = false;
			for( int i = 0; i < allPoints.Length; i++ )
			{
				BezierPoint point = allPoints[i];
				if( ( point.position - point.precedingControlPointPosition ).sqrMagnitude < CONTROL_POINTS_MINIMUM_SAFE_DISTANCE_SQR ||
					( point.position - point.followingControlPointPosition ).sqrMagnitude < CONTROL_POINTS_MINIMUM_SAFE_DISTANCE_SQR )
				{
					showControlPointDistanceWarning = true;
					break;
				}
			}

			if( showControlPointDistanceWarning )
				EditorGUILayout.HelpBox( "Positions of control point(s) shouldn't be very close to (0,0,0), this might result in unpredictable behaviour while moving along the spline with constant speed.", MessageType.Warning );

			EditorGUILayout.Space();

			if( GUILayout.Button( "Swap Control Points" ) )
			{
				for( int i = 0; i < allPoints.Length; i++ )
				{
					Undo.RecordObject( allPoints[i], "Swap Control Points" );

					Vector3 temp = allPoints[i].precedingControlPointLocalPosition;
					allPoints[i].precedingControlPointLocalPosition = allPoints[i].followingControlPointLocalPosition;
					allPoints[i].followingControlPointLocalPosition = temp;
				}

				SceneView.RepaintAll();
			}

      ///////////////////
      //// LINKED NODES

      EditorGUILayout.Space();

      EditorGUILayout.LabelField("Edit linked points", EditorStyles.boldLabel);

      BezierPoint selectedPoint = allPoints[0];

      GUIStyle style = new GUIStyle();

      style.fontSize = 8;
      style.fontStyle = FontStyle.Bold;

      if (allPoints.Length > 1)
      {
        EditorGUILayout.HelpBox("WARNING! Displaying information for only 1 selected point!", MessageType.Warning);
      }

      if (selectedPoint.linkedNodesManager.Number_Of_Linked_Points == 0)
      {
        EditorGUILayout.HelpBox("No linked points connected, add one below.", MessageType.Info);
      }

      for (int i = 0; i < selectedPoint.linkedNodesManager.Number_Of_Linked_Points; ++i)
      {
        string labelName = "Linked Point " + (i + 1).ToString();

        // This is the current linked node we are looking at of the selected point
        LinkedNode currentLinkedNode = selectedPoint.linkedNodesManager.linkedNodes[i];

        currentLinkedNode.Show_Information = EditorGUILayout.BeginFoldoutHeaderGroup(currentLinkedNode.Show_Information, labelName);

        GUILayoutOption maxWidth = GUILayout.MaxWidth(30f);
        GUILayoutOption maxHeight = GUILayout.MaxHeight(7f);

        if (currentLinkedNode.Show_Information)
        {
          GUI.enabled = false;

          EditorGUILayout.BeginHorizontal();

          EditorGUILayout.ObjectField(currentLinkedNode.linkedPoint, typeof(BezierPoint), false, GUILayout.MaxWidth(150f));

          GUI.enabled = true;
          Color temp = GUI.color;
          GUI.color = Color.red;

          if (!GUILayout.Button("", GUILayout.MaxWidth(10f), GUILayout.MaxHeight(10f)))
          {
            GUI.color = temp;

            EditorGUILayout.LabelField("Remove", style, GUILayout.MaxWidth(50f));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Links: ", style, maxWidth, maxHeight);

            // This is the linked node of the current linked node's point we are looking at
            LinkedNode otherLinkedNode = currentLinkedNode.linkedPoint.linkedNodesManager.GetLinkedNode(selectedPoint);
            bool toggleCheck;

            // Y Position Link
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Y", style, maxWidth, maxHeight);

            EditorGUI.BeginChangeCheck();
            toggleCheck = EditorGUILayout.Toggle(currentLinkedNode.linkedNodeBehaviour.linkYPosition);
            if (EditorGUI.EndChangeCheck())
            {
              Undo.RecordObject(selectedPoint, "Change Linked Node Behaviour");
              Undo.RecordObject(selectedPoint.transform, "Change Linked Node Behaviour");
              Undo.RecordObject(currentLinkedNode.linkedPoint, "Change Linked Node Behaviour");

              currentLinkedNode.linkedNodeBehaviour.linkYPosition = toggleCheck;
              otherLinkedNode.linkedNodeBehaviour.linkYPosition = toggleCheck;

              // If setting to true, snap the selected point's values to link to the linked point's value
              if (currentLinkedNode.linkedNodeBehaviour.linkYPosition)
              {
                selectedPoint.transform.position = currentLinkedNode.linkedPoint.transform.position;
              }
            }
            
            EditorGUILayout.EndVertical();

            // Rotation Link
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Rotation", style, maxWidth, maxHeight);

            EditorGUI.BeginChangeCheck();
            toggleCheck = EditorGUILayout.Toggle(currentLinkedNode.linkedNodeBehaviour.linkRotation);
            if (EditorGUI.EndChangeCheck())
            {
              Undo.RecordObject(selectedPoint, "Change Linked Node Behaviour");
              Undo.RecordObject(selectedPoint.transform, "Change Linked Node Behaviour");
              Undo.RecordObject(currentLinkedNode.linkedPoint, "Change Linked Node Behaviour");

              currentLinkedNode.linkedNodeBehaviour.linkRotation = toggleCheck;
              otherLinkedNode.linkedNodeBehaviour.linkRotation = toggleCheck;

              // If setting to true, snap the selected point's values to link to the linked point's value
              if (currentLinkedNode.linkedNodeBehaviour.linkRotation)
              {
                selectedPoint.transform.rotation = currentLinkedNode.linkedPoint.transform.rotation;
              }
            }

            EditorGUILayout.EndVertical();

            // Scale Link
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Scale", style, maxWidth, maxHeight);

            EditorGUI.BeginChangeCheck();
            toggleCheck = EditorGUILayout.Toggle(currentLinkedNode.linkedNodeBehaviour.linkScale);
            if (EditorGUI.EndChangeCheck())
            {
              Undo.RecordObject(selectedPoint, "Change Linked Node Behaviour");
              Undo.RecordObject(selectedPoint.transform, "Change Linked Node Behaviour");
              Undo.RecordObject(currentLinkedNode.linkedPoint, "Change Linked Node Behaviour");

              currentLinkedNode.linkedNodeBehaviour.linkScale = toggleCheck;
              otherLinkedNode.linkedNodeBehaviour.linkScale = toggleCheck;

              // If setting to true, snap the selected point's values to link to the linked point's value
              if (currentLinkedNode.linkedNodeBehaviour.linkScale)
              {
                selectedPoint.transform.localScale = currentLinkedNode.linkedPoint.transform.localScale;
              }
            }

            EditorGUILayout.EndVertical();

            // Control Points Link
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("CP", style, maxWidth, maxHeight);

            EditorGUI.BeginChangeCheck();
            toggleCheck = EditorGUILayout.Toggle(currentLinkedNode.linkedNodeBehaviour.linkControlPoints);
            if (EditorGUI.EndChangeCheck())
            {
              Undo.RecordObject(selectedPoint, "Change Linked Node Behaviour");
              Undo.RecordObject(currentLinkedNode.linkedPoint, "Change Linked Node Behaviour");

              // If setting to true, snap the selected point's values to link to the linked point's value
              if (toggleCheck)
              {
                // Since control points are dependent on each other, set the values first before setting them to linked otherwise it will be wonky
                selectedPoint.handleMode = currentLinkedNode.linkedPoint.handleMode;

                selectedPoint.precedingControlPointPosition = currentLinkedNode.linkedPoint.precedingControlPointPosition;
                selectedPoint.followingControlPointPosition = currentLinkedNode.linkedPoint.followingControlPointPosition;
              }

              currentLinkedNode.linkedNodeBehaviour.linkControlPoints = toggleCheck;
              otherLinkedNode.linkedNodeBehaviour.linkControlPoints = toggleCheck;
            }

            EditorGUILayout.EndVertical();

            // Camera Behaviour Link
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("CB", style, maxWidth, maxHeight);
            EditorGUILayout.Toggle(currentLinkedNode.linkedNodeBehaviour.linkCameraBehaviour);

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
          }

          else
          {
            selectedPoint.linkedNodesManager.RemoveLinkedPoint(currentLinkedNode.linkedPoint);
          }
        }

        EditorGUILayout.Space();

        EditorGUILayout.EndFoldoutHeaderGroup();
      }

      if (selectedPoint.Show_Add_Linked_Node_Menu)
      {
        ActiveEditorTracker.sharedTracker.isLocked = true;

        BezierUtils.DrawSeparator();

        EditorGUILayout.LabelField("Add linked point", EditorStyles.boldLabel);

        selectedPoint.Add_Linked_Point_Field_Value = (BezierPoint)EditorGUILayout.ObjectField("Linked Point", selectedPoint.Add_Linked_Point_Field_Value, typeof(BezierPoint), true);

        EditorGUILayout.Space();

        LinkedNodeBehaviour linkedPointBehaviourOptions = selectedPoint.Linked_Point_Behaviour_Options;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All")) linkedPointBehaviourOptions.CheckAll();
        if (GUILayout.Button("Unselect All")) linkedPointBehaviourOptions.UncheckAll();

        EditorGUILayout.EndHorizontal();

        linkedPointBehaviourOptions.linkYPosition = EditorGUILayout.Toggle("Link Y Position", linkedPointBehaviourOptions.linkYPosition);
        linkedPointBehaviourOptions.linkRotation = EditorGUILayout.Toggle("Link Rotation", linkedPointBehaviourOptions.linkRotation);
        linkedPointBehaviourOptions.linkScale = EditorGUILayout.Toggle("Link Scale", linkedPointBehaviourOptions.linkScale);
        linkedPointBehaviourOptions.linkControlPoints = EditorGUILayout.Toggle("Link Control Points", linkedPointBehaviourOptions.linkControlPoints);
        linkedPointBehaviourOptions.linkCameraBehaviour = EditorGUILayout.Toggle("Link Camera Behaviour", linkedPointBehaviourOptions.linkCameraBehaviour);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        bool linkButtonPressed = false;
        bool linkPointTo = true;

        if (GUILayout.Button("Link to this point"))
        {
          linkButtonPressed = true;
          linkPointTo = true;
        }

        if (GUILayout.Button("Link to input point"))
        {
          linkButtonPressed = true;
          linkPointTo = false;
        }

        if (linkButtonPressed)
        {
          LinkageErrors linkageErrorCheck = selectedPoint.linkedNodesManager.AddLinkedPoint(
            selectedPoint,
            selectedPoint.Add_Linked_Point_Field_Value,
            linkedPointBehaviourOptions,
            linkPointTo
            );

          if (linkageErrorCheck != LinkageErrors.Success)
          {
            switch (linkageErrorCheck)
            {
              case LinkageErrors.NullError:
                EditorUtility.DisplayDialog("Error linking point!", "Tried to link a null value, insert a BezierPoint into the Linked Point field before linking.", "Okay");
                break;

              case LinkageErrors.EqualError:
                EditorUtility.DisplayDialog("Error linking point!", "Tried to link the same point to itself.", "Okay");
                break;

              case LinkageErrors.SameSplineError:
                EditorUtility.DisplayDialog("Error linking point!", "Tried to link two points that exist on the same spline. You can only link points from different splines.", "Okay");
                break;

              case LinkageErrors.ExistsInChainError:
                EditorUtility.DisplayDialog("Error linking point!", "Tried to link a point that is already linked with a point inside the list of linked points on this point or further in the chain.", "Okay");
                break;
            }
          }

          else
          {
            ActiveEditorTracker.sharedTracker.isLocked = false;

            selectedPoint.Show_Add_Linked_Node_Menu = false;
          }

          selectedPoint.Add_Linked_Point_Field_Value = null;

          SceneView.RepaintAll();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        Color temp = GUI.color;

        GUI.color = Color.red;

        if (GUILayout.Button("Cancel"))
        {
          ActiveEditorTracker.sharedTracker.isLocked = false;

          selectedPoint.Show_Add_Linked_Node_Menu = false;
          selectedPoint.Add_Linked_Point_Field_Value = null;
        }

        GUI.color = temp;
      }

      else
      {
        selectedPoint.Show_Add_Linked_Node_Menu = GUILayout.Button("Add linked point");
        selectedPoint.Add_Linked_Point_Field_Value = null;
      }

      //// END
      /////////

      EditorGUILayout.Space();
			BezierUtils.DrawSeparator();

			hasMultipleDifferentValues = false;
			BezierPoint.ExtraData extraData = allPoints[0].extraData;
			for( int i = 1; i < allPoints.Length; i++ )
			{
				if( allPoints[i].extraData != extraData )
				{
					hasMultipleDifferentValues = true;
					break;
				}
			}

			GUILayout.BeginHorizontal();
			EditorGUI.showMixedValue = hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();
			Rect extraDataRect = EditorGUILayout.GetControlRect( false, EditorGUIUtility.singleLineHeight ); // When using GUILayout, button isn't vertically centered
			extraDataRect.width -= 65f;
			extraData = EditorGUI.Vector4Field( extraDataRect, "Extra Data", extraData );
			extraDataRect.x += extraDataRect.width + 5f;
			extraDataRect.width = 30f;
			if( GUI.Button( extraDataRect, EXTRA_DATA_SET_AS_CAMERA ) )
				extraData = SceneView.lastActiveSceneView.camera.transform.rotation;
			if( EditorGUI.EndChangeCheck() )
			{
				for( int i = 0; i < allPoints.Length; i++ )
				{
					Undo.RecordObject( allPoints[i], "Change Extra Data" );

					allPoints[i].extraData = extraData;
				}

				SceneView.RepaintAll();
			}

			EditorGUI.showMixedValue = false;

			extraDataRect.x += 30f;
			EditorGUI.BeginChangeCheck();
			m_visualizeExtraDataAsFrustum = GUI.Toggle( extraDataRect, m_visualizeExtraDataAsFrustum, EXTRA_DATA_VIEW_AS_FRUSTUM, GUI.skin.button );
			if( EditorGUI.EndChangeCheck() )
			{
				VisualizeExtraDataAsFrustum = m_visualizeExtraDataAsFrustum;
				SceneView.RepaintAll();
			}

			GUILayout.EndHorizontal();

			BezierUtils.DrawSeparator();
			EditorGUILayout.Space();

			Color c = GUI.color;
			GUI.color = RESET_POINT_BUTTON_COLOR;

			if( GUILayout.Button( "Reset Point" ) )
			{
				for( int i = 0; i < allPoints.Length; i++ )
				{
					Undo.RecordObject( allPoints[i].transform, "Reset Point" );
					Undo.RecordObject( allPoints[i], "Reset Point" );

					allPoints[i].Reset();
				}

				SceneView.RepaintAll();
			}

			EditorGUILayout.Space();

			GUI.color = REMOVE_POINT_BUTTON_COLOR;

			if( GUILayout.Button( "Remove Point" ) )
			{
				RemoveSelectedPoints();
				GUIUtility.ExitGUI();
			}

			GUI.color = c;

			for( int i = 0; i < allSplines.Length; i++ )
				allSplines[i].Internal_CheckDirty();

      /////////////////////////////
      //// CAMERA BEHAVIOUR LINKER
      //for (int i = 0; i < allPoints.Length; i++)
      //{
      //  if (allPoints[i].linkedPoint && !allPoints[i].cameraBehaviour.Equal(allPoints[i].linkedPoint.cameraBehaviour))
      //  {
      //    Undo.RecordObject(allPoints[i].linkedPoint.cameraBehaviour, "Change Linked Camera Behaviour");

      //    allPoints[i].linkedPoint.cameraBehaviour.CopyFrom(allPoints[i].cameraBehaviour);
      //  }
      //}
    }

		private bool CheckCommands()
		{
			Event e = Event.current;
			if( e.type == EventType.ValidateCommand )
			{
				if( e.commandName == "Delete" )
				{
					RemoveSelectedPoints();
					e.type = EventType.Ignore;

					return true;
				}
				else if( e.commandName == "Duplicate" )
				{
					DuplicateSelectedPoints();
					e.type = EventType.Ignore;

					return true;
				}
			}

			if( e.isKey && e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete )
			{
				RemoveSelectedPoints();
				e.Use();

				return true;
			}

			return false;
		}

		private void InsertNewPoints( bool insertAfter )
		{
			Undo.IncrementCurrentGroup();

			Object[] newSelection = new Object[pointCount];
			for( int i = 0, index = 0; i < selection.Length; i++ )
			{
				BezierSpline spline = selection[i].spline;
				BezierPoint[] points = selection[i].points;

				for( int j = 0; j < points.Length; j++ )
				{
					BezierPoint newPoint;
					if( spline )
					{
						int pointIndex = points[j].Internal_Index;
						if( insertAfter )
							pointIndex++;

						Vector3 position;
						if( spline.Count >= 2 )
						{
							if( pointIndex > 0 && pointIndex < spline.Count )
								position = ( spline[pointIndex - 1].localPosition + spline[pointIndex].localPosition ) * 0.5f;
							else if( pointIndex == 0 )
							{
								if( spline.loop )
									position = ( spline[0].localPosition + spline[spline.Count - 1].localPosition ) * 0.5f;
								else
									position = spline[0].localPosition - ( spline[1].localPosition - spline[0].localPosition ) * 0.5f;
							}
							else
							{
								if( spline.loop )
									position = ( spline[0].localPosition + spline[spline.Count - 1].localPosition ) * 0.5f;
								else
									position = spline[pointIndex - 1].localPosition + ( spline[pointIndex - 1].localPosition - spline[pointIndex - 2].localPosition ) * 0.5f;
							}
						}
						else if( spline.Count == 1 )
							position = pointIndex == 0 ? spline[0].localPosition - Vector3.forward : spline[0].localPosition + Vector3.forward;
						else
							position = Vector3.zero;

						newPoint = spline.InsertNewPointAt( pointIndex );
						newPoint.localPosition = position;
					}
					else
						newPoint = Instantiate( points[j], points[j].transform.parent );

					Undo.RegisterCreatedObjectUndo( newPoint.gameObject, "Insert Point" );
					if( newPoint.transform.parent )
						Undo.RegisterCompleteObjectUndo( newPoint.transform.parent, "Insert Point" );

					newSelection[index++] = newPoint.gameObject;
				}
			}

			Selection.objects = newSelection;
			SceneView.RepaintAll();
    }

    private void CreateTempPointAlongSpline()
    {
      Undo.IncrementCurrentGroup();

      BezierSpline spline = selection[0].spline;

      Undo.RegisterCompleteObjectUndo(spline, "Create Temp Point");

      BezierPoint selectedPoint = selection[0].points[0];

      spline.tempPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
      spline.tempPoint.name = "Temp Point";
      spline.tempPoint.transform.position = selectedPoint.transform.position;
      spline.tempPoint.transform.localScale = new Vector3(0.2f, 1, 0.2f);
      spline.tempPoint.transform.parent = selectedPoint.transform.parent;

      Undo.RegisterCreatedObjectUndo(spline.tempPoint, "Create Temp Point");

      spline.movingPointAlongSpline = true;

      SceneView.RepaintAll();
    }

    private void DuplicateSelectedPoints()
		{
			Undo.IncrementCurrentGroup();

			Object[] newSelection = new Object[pointCount];
			for( int i = 0, index = 0; i < selection.Length; i++ )
			{
				BezierSpline spline = selection[i].spline;
				BezierPoint[] points = selection[i].points;

				for( int j = 0; j < points.Length; j++ )
				{
					BezierPoint newPoint;
					if( spline )
						newPoint = spline.DuplicatePointAt( points[j].Internal_Index );
					else
						newPoint = Instantiate( points[j], points[j].transform.parent );

					Undo.RegisterCreatedObjectUndo( newPoint.gameObject, "Duplicate Point" );
					if( newPoint.transform.parent )
						Undo.RegisterCompleteObjectUndo( newPoint.transform.parent, "Duplicate Point" );

					newSelection[index++] = newPoint.gameObject;
				}
			}

			Selection.objects = newSelection;
			SceneView.RepaintAll();
		}

		private void RemoveSelectedPoints()
		{
			Undo.IncrementCurrentGroup();

			Object[] newSelection = new Object[selection.Length];
			for( int i = 0; i < selection.Length; i++ )
			{
				BezierSpline spline = selection[i].spline;
				BezierPoint[] points = selection[i].points;

				for( int j = 0; j < points.Length; j++ )
					Undo.DestroyObjectImmediate( points[j].gameObject );

				if( spline )
					newSelection[i] = spline.gameObject;
			}

			Selection.objects = newSelection;
			SceneView.RepaintAll();
		}

		private void OnUndoRedo()
		{
			controlPointRotationsInitialized = false;

			for( int i = 0; i < selection.Length; i++ )
			{
				if( selection[i].spline )
					selection[i].spline.Refresh();
			}

			Repaint();
		}

		private bool HasFrameBounds()
		{
			return !serializedObject.isEditingMultipleObjects;
		}

		private Bounds OnGetFrameBounds()
		{
			return new Bounds( ( (BezierPoint) target ).position, new Vector3( 1f, 1f, 1f ) );
		}
  }
}