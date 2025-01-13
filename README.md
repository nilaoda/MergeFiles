# MergeFiles
二进制合并TS小工具

效果等价于以下命令：
```
copy /b *.ts MERGED_FILE
```

程序的“排序”功能调用了 Win32API `StrCmpLogicalW` 实现自然排序，在挂载网络驱动器的场景下特别有用。

## 截图
![img](./SC.png)

## 演示
https://github.com/user-attachments/assets/60c94191-6c6e-4241-8846-ebafc7590bd6