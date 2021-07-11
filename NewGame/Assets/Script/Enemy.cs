using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : LivingEntity
{
	public enum State { Idle, Chasing, Attacking };//trang thái của enemy
	State currentState;

	public ParticleSystem deathEffect;

	NavMeshAgent pathfinder;
	Transform target;
	LivingEntity targetEntity;
	Material skinMaterial;

	Color originalColour;

	float attackDistanceThreshold = .5f;//kc tan cong
	float timeBetweenAttacks = 1;//tgian mỗi đợt tấn công
	float damage = 1;

	float nextAttackTime;
	float myCollisionRadius;//đại diện cho kthuc
	float targetCollisionRadius;//pham vi va chạm của mục tiêu

	bool hasTarget;

	protected override void Start()
	{
		base.Start();
		pathfinder = GetComponent<NavMeshAgent>();
		skinMaterial = GetComponent<Renderer>().material;
		originalColour = skinMaterial.color;

		if (GameObject.FindGameObjectWithTag("Player") != null)
		{
			currentState = State.Chasing;
			hasTarget = true;

			target = GameObject.FindGameObjectWithTag("Player").transform;
			targetEntity = target.GetComponent<LivingEntity>();
			targetEntity.OnDeath += OnTargetDeath;

			myCollisionRadius = GetComponent<CapsuleCollider>().radius;
			targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;

			StartCoroutine(UpdatePath());
		}
	}

	public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
	{
		if (damage >= health)
		{
			Destroy(Instantiate(deathEffect.gameObject, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection)) as GameObject, deathEffect.startLifetime);//khi quái sinh ra thì hiệu ứng chết mất đi

		}
		base.TakeHit(damage, hitPoint, hitDirection);
	}

	void OnTargetDeath()
	{
		hasTarget = false;
		currentState = State.Idle;
	}

	void Update()
	{

		if (hasTarget)
		{
			if (Time.time > nextAttackTime)
			{
				float sqrDstToTarget = (target.position - transform.position).sqrMagnitude;//(vitri kthu - vị trí sau khi thay đổi)//bình phương khoảng cách tới mục tiêu
				if (sqrDstToTarget < Mathf.Pow(attackDistanceThreshold + myCollisionRadius + targetCollisionRadius, 2))//ktra kc người vs kthu nhỏ hơn ngưởng kcach
				{
					nextAttackTime = Time.time + timeBetweenAttacks;//tgian tấn công
					StartCoroutine(Attack());
				}

			}
		}

	}

	IEnumerator Attack()
	{
		//trong cuộc tấn công nếu chỉ định  currentState = State.Attacking thì UpdatePath() k đc thực thi
		currentState = State.Attacking;
		pathfinder.enabled = false;

		Vector3 originalPosition = transform.position;
		Vector3 dirToTarget = (target.position - transform.position).normalized;
		Vector3 attackPosition = target.position - dirToTarget * (myCollisionRadius);//điểm đích// trừ để đòn tấn công k qua ng

		float attackSpeed = 3;
		float percent = 0;

		skinMaterial.color = Color.red;//chuyển sang đỏ khi tấn công
		bool hasAppliedDamage = false;

		while (percent <= 1)
		{

			if (percent >= .5f && !hasAppliedDamage)
			{
				hasAppliedDamage = true;
				targetEntity.TakeDamage(damage);
			}

			percent += Time.deltaTime * attackSpeed;
			float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
			transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);
			//khi nội suy = 0 là ở vị trí bắt đầu, 1 là ở vị trí tấn công
			yield return null;
		}

		skinMaterial.color = originalColour;
		//khi cuộc tấn công kết thức nó sẽ quay lại hiện tại currentState = State.Chasing;
		currentState = State.Chasing;
		pathfinder.enabled = true;
	}
	
	IEnumerator UpdatePath()
	{
		float refreshRate = .25f;

		while (hasTarget)
		{
			if (currentState == State.Chasing)
			{
				Vector3 dirToTarget = (target.position - transform.position).normalized;
				Vector3 targetPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold / 2);
				                                           // tổng của vecto hướng tới mục tiêu nhân với kthu  và bán kính va chạm của chính nó và 1/2 kc tấn công	
				if (!dead)
				{
					pathfinder.SetDestination(targetPosition);
				}
			}
			yield return new WaitForSeconds(refreshRate);
		}
	}
}
 