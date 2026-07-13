馬達煞車線要接
然後motor 相位矯正
才可sever on
做回home

這次嘗試載入.cmd及z軸一維校正檔
後進行一般的home設定時出現問題
會碰到極限出現錯誤
總結錯誤原因
1.加速度太小碰到極限時來不及停下來而超出極限出現錯誤
2.z軸一維校正檔因為會進行補償所以會超出極限出現錯誤

所以跟原廠買軸
有給.cmd檔及.cal校正檔
直接載就可以用
原廠的設置會出現在Advanced裡面
不用再特別用Basic的回home功能進行設定





這次是單買z軸而已所以附贈的.cmd
只有z軸調好的參數
後續要做加入xy平台

可以將xy 平台當前的.cmd下載下來
然後帶到new Lcn automation1進行檔案比較
並且將new Lcn automation1空的部分透過xy平台的.cmd檔進行寫入到new Lcn automation1


校正檔的軸需要是從1開始
而驅動器的channel 是從0開始
因此z軸的channel是2而z軸的一維校正檔裡面是3




# test