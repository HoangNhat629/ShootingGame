using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent (typeof(Rigidbody))]

public class PlayController : MonoBehaviour
{
    Vector3 velocity; //vận tốc
    Rigidbody myRigidBody;//lam doi tuong di chuyen de no chiu anh huong boi va cham ta dung RigiBody
    void Start()
    {
        myRigidBody = GetComponent<Rigidbody>();
    }
    //chuyen dong doi tuong theo van toc cua no     
    public void Move (Vector3 _velocity)
    {   
      velocity = _velocity;     
    }
    public void LookAt(Vector3 lookPoint)
    {
        Vector3 heightCorrectedPoint = new Vector3(lookPoint.x, transform.position.y, lookPoint.z);
        transform.LookAt(heightCorrectedPoint);
    }
    //di chuyển RigiBody để đc chạy thường xuyên và lặp lại trong khoảng không gian ngắn => k bị kẹt dưới các vật thể khác
    //Khi cảy ra hiện tượng sụt khung hình, khung hình sẽ đc nhân với trọng lượng thời gian và đc thực thi để duy trì tốc đồ di chuyển
   void FixedUpdate (){
         myRigidBody.MovePosition(myRigidBody.position + velocity * Time.fixedDeltaTime);
         //vị trí hiện tại của myRigidBody + vận tốc * khoảng thời gian giữa hai phương thức FixedUpdate đc gọi
    }
}
