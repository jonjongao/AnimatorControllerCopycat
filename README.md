# AnimatorCopycat
# Copycat Workflow #
- **Template**放入要作為模板的*Animator Controller*
- 如果是首次使用此模板, 要點選**Start Edit**才能繼續下一步
- **Target Model Root**放入欲置換目標的*Fbx根模型*
	- i.e. robot.fbx, robot@idle.fbx, robot@attack.fbx. 你應該放入*robot.fbx*
- **Contract**設定要搜尋的*Animation名稱*, 搜尋目標將以Target Model Root為依據做搜尋
	- i.e. 根模型為robot, 如果要搜尋robot@attack, 你應該輸入*attack*
- 當左起第一個燈號顯示**綠燈**, 表示目標動畫有搜尋到
- 如果Template有設定, 會出現**assign to...**選項, 選擇你欲置換的*State*
- 設定完後, 選擇**Apply**或者**Apply All** 將設定賦予目標, 設定成功的State第二個燈號會亮**綠燈**
- 設定完後可以點選**Save As Preset**儲存這次Contract的設定(Animation名稱和State目標), 以供下次編輯使用
- 選擇**Save**將這次的編輯輸出儲存為成品**Animator Controller**
	- 勾選**Keep Edit Data**, Save完後會繼續保留編輯資料, 供快速製作用

![](http://i.imgur.com/WAyCSHX.png)
![](http://i.imgur.com/ihMlfSs.png)
![](http://i.imgur.com/CxoBwG9.png)
![](http://i.imgur.com/suzFFQN.png)
