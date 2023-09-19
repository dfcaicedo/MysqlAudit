Imports MySql.Data.MySqlClient
Imports System.IO
Public Class Form1
    Dim cnt As MySqlConnection
    Dim cnt2 As MySqlConnection
    Dim host As String
    Dim user As String
    Dim pass As String
    Dim bdd As String
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try

            Me.host = Me.TextBox1.Text
            Me.user = Me.TextBox2.Text
            Me.pass = Me.TextBox3.Text
            Me.bdd = Me.TextBox4.Text
            cnt = New MySqlConnection("Server=" & Me.host & ";Database=" & Me.bdd & ";Uid= " & Me.user & ";Pwd=" & Me.pass & ";")
            cnt2 = New MySqlConnection("Server=" & Me.host & ";Database=" & Me.bdd & ";Uid= " & Me.user & ";Pwd=" & Me.pass & ";")
            cnt.Open()
            cnt.Close()
            MsgBox("La conexión ha sido exitosa")
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim sql As String
        Dim ltablas As MySqlDataReader
        Dim lcampos As MySqlDataReader
        Dim cmdt As MySqlCommand
        Dim cmdc As MySqlCommand
        Button1_Click(sender, e)
        Try
            cnt.Open()
            sql = " drop table if EXISTS  au_dtran;" & vbCrLf
            sql &= "drop table if EXISTS  au_trans;" & vbCrLf
            sql &= "CREATE TABLE IF NOT EXISTS `au_trans` (" & vbCrLf
            sql &= "    `tra_cont` bigint(20) NOT NULL AUTO_INCREMENT," & vbCrLf
            sql &= "    `tra_tabl` varchar(500) NOT NULL," & vbCrLf
            sql &= "    `tra_acci` varchar(50) NOT NULL," & vbCrLf
            sql &= "    `tra_user` varchar(50) NOT NULL," & vbCrLf
            sql &= "    `tra_fech` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP," & vbCrLf
            sql &= "    PRIMARY KEY (`tra_cont`)" & vbCrLf
            sql &= ") ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;" & vbCrLf
            sql &= "CREATE TABLE IF NOT EXISTS `au_dtran` (" & vbCrLf
            sql &= "    `dtr_cont` bigint(20) NOT NULL AUTO_INCREMENT," & vbCrLf
            sql &= "    `tra_cont` bigint(20) NOT NULL," & vbCrLf
            sql &= "    `dtr_fiel` varchar(1500)  NULL," & vbCrLf
            sql &= "    `dtr_vnew` varchar(1500)  NULL," & vbCrLf
            sql &= "    `dtr_vold` varchar(1500)  NULL," & vbCrLf
            sql &= "    PRIMARY KEY (`dtr_cont`)" & vbCrLf
            sql &= ") ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;" & vbCrLf
            sql &= "delimiter //" & vbCrLf
            sql &= "drop trigger if exists eliminarduplicados_trg //" & vbCrLf
            sql &= "ALTER TABLE `au_dtran`" & vbCrLf
            sql &= "ADD INDEX `transacion` (`tra_cont` ASC);" & vbCrLf
            sql &= "ALTER TABLE `au_dtran` " & vbCrLf
            sql &= "ADD CONSTRAINT `transacion_fk`" & vbCrLf
            sql &= "FOREIGN KEY (`tra_cont`)" & vbCrLf
            sql &= "REFERENCES `au_trans` (`tra_cont`)" & vbCrLf
            sql &= "ON DELETE cascade" & vbCrLf
            sql &= "ON UPDATE NO ACTION;" & vbCrLf

            cmdt = New MySqlCommand("show full tables where table_type ='BASE TABLE'", Me.cnt)
            ltablas = cmdt.ExecuteReader
            While ltablas.Read
                Dim tabla As String
                tabla = ltablas.GetValue(0)
                If tabla <> "au_trans" And tabla <> "au_dtran" Then
                    cnt2.Open()
                    cmdc = New MySqlCommand("desc `" & tabla & "`", cnt2)
                    Dim campos() As Fields
                    campos = Nothing

                    lcampos = cmdc.ExecuteReader
                    While lcampos.Read
                        If campos Is Nothing Then
                            ReDim campos(0)
                        Else
                            ReDim Preserve campos(campos.Length)
                        End If
                        campos(campos.Length - 1) = New Fields
                        campos(campos.Length - 1).name = lcampos.GetValue(0)
                        campos(campos.Length - 1).key = False

                        If lcampos.GetString(3).Trim = "PRI" Or lcampos.GetString(3).Trim = "UNI" Then
                            campos(campos.Length - 1).key = True
                        End If
                    End While
                    lcampos.Close()
                    cnt2.Close()
                    sql &= "drop trigger if exists `" & tabla & "_insert_audit` //" & vbCrLf
                    sql &= " create trigger " & tabla & "_insert_audit after insert on `" & tabla & "`" & vbCrLf
                    sql &= "    for each row" & vbCrLf
                    sql &= "    begin" & vbCrLf
                    sql &= "        insert into au_trans values(null,'" & tabla & "','INSERT',USER(),now());" & vbCrLf
                    For p = 0 To campos.Length - 1
                        sql &= "        insert into au_dtran values(null,(select max(tra_cont) from au_trans),'" & campos(p).name & "',new.`" & campos(p).name & "`,'');" & vbCrLf
                    Next
                    sql &= " end " & vbCrLf
                    sql &= "//" & vbCrLf
                    sql &= "drop trigger if exists " & tabla & "_update_audit //" & vbCrLf
                    sql &= " create trigger " & tabla & "_update_audit before update on " & tabla & vbCrLf
                    sql &= "    for each row" & vbCrLf
                    sql &= "    begin" & vbCrLf
                    sql &= "        insert into au_trans values(null,'" & tabla & "','UPDATE',USER(),now());" & vbCrLf
                    For p = 0 To campos.Length - 1
                        If Not campos(p).key Then
                            sql &= " if New.`" & campos(p).name & "` <> Old.`" & campos(p).name & "` then" & vbNewLine
                            sql &= "        insert into au_dtran values(null,(select max(tra_cont) from au_trans),'" & campos(p).name & "',new.`" & campos(p).name & "`,old.`" & campos(p).name & "`);" & vbCrLf
                            sql &= "end if;" & vbNewLine
                        Else
                            sql &= "        insert into au_dtran values(null,(select max(tra_cont) from au_trans),'" & campos(p).name & "',new.`" & campos(p).name & "`,old.`" & campos(p).name & "`);" & vbCrLf
                        End If

                    Next
                    sql &= " end " & vbCrLf
                    sql &= "//" & vbCrLf
                    sql &= "drop trigger if exists " & tabla & "_delete_audit //" & vbCrLf
                    sql &= " create trigger " & tabla & "_delete_audit before delete on `" & tabla & "`" & vbCrLf
                    sql &= "    for each row" & vbCrLf
                    sql &= "    begin" & vbCrLf
                    sql &= "        insert into au_trans values(null,'" & tabla & "','DELETE',USER(),now());" & vbCrLf
                    For p = 0 To campos.Length - 1
                        sql &= "        insert into au_dtran values(null,(select max(tra_cont) from au_trans),'" & campos(p).name & "','',old.`" & campos(p).name & "`);" & vbCrLf
                    Next
                    sql &= " end " & vbCrLf
                    sql &= "//" & vbCrLf
                End If
            End While
            ltablas.Close()
            Me.cnt.Close()
            Dim qq As StreamWriter
            qq = New StreamWriter("auditscript.sql")
            sql &= "delimiter ;" & vbCrLf
            qq.Write(sql)
            qq.Close()
            Shell("explorer.exe auditscript.sql")
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

    End Sub
End Class
