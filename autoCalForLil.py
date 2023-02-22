
from datetime import datetime
import io
f = io.open("TraceLog_2023-02-21.xrslog", mode="r", encoding="utf-8")
# li = []
li3 = []
for i in f.readlines():
    if "Hardware positions calculated: 3" in i:
        print("{}, {}, {}".format(i.split('§')[0], i.split('§')[1], i.split('§')[9]))
        li = []
        li2 = []
        count = 0
    if "[User][XraySwitch] PrepButtonPressed. Source: generator." in i:
        if count == 0:
            # print(i.split('§')[9])
            li.append(i.split('§')[1])
            count += 1

    if "Received message:XR2" in i:
        # print(i.split('§')[9])
        li.append(i.split('§')[1])
        # print(li)
        if len(li) == 4:
            FMT = '%Y-%m-%d %H:%M:%S.%f'
            for k in range(0, 3):
                delta = datetime.strptime(li[k+1], FMT) - datetime.strptime(li[k], FMT)
                li2.append(delta.total_seconds())
                li3.append(delta.total_seconds())
                print(datetime.strptime(li[k+1], FMT) - datetime.strptime(li[k], FMT))

                # sum(li2, datetime.timedelta(0)) / len(li2)
                # print("On Average:", sum(li2, datetime.timedelta()) / len(li2))
            # print("On Average", sum(li2) / len(li2))

i = 0
print("第一次 prepButton 到 第一次 XR2 的平均值:",  ((li3[i] + li3[i+3] + li3[i+6]) / 3))
i = 1
print("第一次 XR2 到 第二次 XR2 的平均值:", ((li3[i] + li3[i + 3] + li3[i + 6]) / 3))
i = 2
print("第二次 XR2 到 第三次 XR2 的平均值:", ((li3[i] + li3[i + 3] + li3[i + 6]) / 3))
