using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(PlayController))]
[RequireComponent(typeof(GunControll))]
public class Player : LivingEntity
{
    Camera viewCamera;
    public float MoveSpeed = 5;
    PlayController controller;
    GunControll gunController;
	// Start is called before the first frame update

	protected override void Start()
	{
		base.Start();
		controller = GetComponent<PlayController>();
        gunController = GetComponent<GunControll>();
        viewCamera = Camera.main;
    }

	// Update is called once per frame
	void Update()
	{
		// Movement input
		Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
		Vector3 moveVelocity = moveInput.normalized * MoveSpeed;
		controller.Move(moveVelocity);

		// Look input
		Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
		Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
		float rayDistance;

		if (groundPlane.Raycast(ray, out rayDistance))
		{
			Vector3 point = ray.GetPoint(rayDistance);
			//Debug.DrawLine(ray.origin,point,Color.red);
			controller.LookAt(point);
		}

		// Weapon input
		if (Input.GetMouseButton(0))
		{
			gunController.Shoot();
		}
	}
}
