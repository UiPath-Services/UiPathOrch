# Outlookのアプリケーションオブジェクトを作成
$outlook = New-Object -ComObject Outlook.Application

# 新しいメールアイテムを作成
$mail = $outlook.CreateItem(0)

# メールの属性を設定
$mail.To = "yoshifumi.tsuda@uipath.com"
$mail.Subject = "Test Email from Outlook"
$mail.Body = "This is a test email sent from Outlook via PowerShell."

# メールを送信
$mail.Send()

# この時点でOutlookが開いていない場合、Outlookはバックグラウンドで起動し、指定された属性でメールを送信します。
