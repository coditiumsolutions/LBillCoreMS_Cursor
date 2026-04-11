	Call SetGetDBs()
	Set dccthis =  dbthis.UnprocessedDocuments
	Set docthis = dcthis.GetFirstDocument
	
	Print "end SetGetDBs()"
	If OperatorSetup = False Then
		Exit Sub
	End If
	Print "end OperatorSetup()"
	
	If LoopGeneration() = False Then
		Exit Sub
	End If


Function SetGetDBs()
	Set session = New NotesSession
	Set ws = New NotesUIWorkspace
	Set dbthis = session.CurrentDatabase
	Set dcthis = dbthis.UnprocessedDocuments	
	Set docProfile = dbthis.GetProfileDocument("ProfileDocument")
	Set viewDuplicate = dbthis.GetView("keyDuplicateBill")	
	Set viewRates = dbthis.GetView("Rates")
	Set viewPreviousBill = dbthis.GetView("KeyPreviousBill")
	pathDbFine = docProfile.DBFinePath(0)
	
	If dbthis.Server = "" Then
		Set dbFine = session.GetDatabase("", "Billing Server Live\FMS.nsf")
		Set viewFine = dbFine.GetView("KeyFineMaint")
		
	Elseif dbthis.Server = "CN=CRMServerBTK2/O=BahriaTown" Then
		Set dbFine = session.GetDatabase(dbthis.Server, "Developers\Billing\Lahore\FMS.nsf")
		Set viewFine = dbFine.GetView("KeyFineMaint")
		
	Else
		Set dbFine = session.GetDatabase(dbthis.Server, pathDbFine)
		Set viewFine = dbFine.GetView("KeyFineMaint")
	End If
	'If Not dbFine Is Nothing Then
	'End If
	
	
	'Set dbFine = session.GetDatabase("","Billing Server Live\FMS.nsf")
	'Set dbFine = session.GetDatabase(dbthis.Server,"Billing BTL\FMS.nsf")
	'Set dbFine = session.GetDatabase(dbthis.Server,"Developers\Billing\Lahore\FMS.nsf")
End Function




Function OperatorSetup() As Boolean
	
	userName = session.Commonusername
	Set viewOptSetup = dbthis.Getview("Operators Setup")
	Set docOptSetup = viewOptSetup.Getdocumentbykey(userName,True)
	If docOptSetup Is Nothing Then
		Msgbox "Please add your name in operator setup",64,"Bahring Town"
		OperatorSetup = False
		Exit Function
	End If
	
	If Cstr(docOptSetup.Billing_Year(0)) = "" Or Cstr(docOptSetup.Billing_Month(0))= "" Then
		Msgbox "Please Add Billing Mongh and Billing Year in Operator Setup",64,"Bahring Town"
		OperatorSetup = False
		Exit Function
	End If
	
	Billing_Month = docOptSetup.Billing_Month(0)
	Billing_Year = docOptSetup.Billing_Year(0)
	Set ValidDate = New NotesDateTime(docOptSetup.ValidDate(0))
	
	Dim dtCurrentMonth As New NotesDateTime(Today)
	If Len(Cstr(Month(Today))	)  = 1  Then
		InvoiceNoLeft = Cstr(Year(Today)) + "0" +  Cstr(Month(Today))
	Else
		InvoiceNoLeft = Cstr(Year(Today)) + Cstr(Month(Today))
	End If	
	
	
	OperatorSetup = True
End Function





Function LoopGeneration() As Boolean
	Set dcthis = dbthis.Unprocesseddocuments
	Set docthis = dcthis.Getfirstdocument()
	TotalBills = 0
	'**************************************
	While Not docthis Is Nothing 
	'***************************************
		If VerifyProcess() = False Then
			Goto NextRecord
		End If 		
		Print "End Verify Process"
		If PreviousBill() = False Then
			Goto NextRecord
		End If 
		'Msgbox "p 11"
		Print "End Previous Bill"
		Call GenerateBill()		
		
NextRecord:
		Set docthis = dcthis.Getnextdocument(docthis)
	Wend	
	Msgbox Cstr(TotalBills) + "  Bill(s) Generated Successfully", 64 , "Bahria Town"
	
	LoopGeneration = False 
End Function


Function VerifyProcess() As Boolean
	'Msgbox session.CommonUserName 
	'If session.CommonUserName = "Shahid Ghauri" Or session.CommonUserName = "Admin" Then
	'	VerifyProcess = True
	'	Exit Function
	'End If
	
	key = docthis.RefrenceNoBarCode(0) + "-" + docOptSetup.Billing_Year(0) + "-" + docOptSetup.Billing_Month(0)
	
	Set dcDuplicate = viewDuplicate.GetAllDocumentsByKey(key,True)
	If dcDuplicate.Count > 0 Then
		docthis.BillGenStatus = "Bill Already Generated-" + Cstr(docOptSetup.Billing_Year(0))  +  "-"+ Cstr(docOptSetup.Billing_Month(0))
		
		Call docthis.Save(True,False)
		VerifyProcess = False
		Exit Function
	End If	
	
	If Cstr(docthis.connectionstatus(0)) = "Disconnected"  Then
		docthis.BillGenStatus = "Disconected Customer" 
		Call docthis.Save(True,False)
		VerifyProcess = False
		Exit Function
	End If
	
'	If Cstr(docthis.excel(0)) = "" And Cstr(docthis.excelPH(0)) = "" And Cstr(docthis.excelOffPH(0)) = ""  Then
'		docthis.BillGenStatus = "Incorrect Reading for " + Cstr(docOptSetup.Billing_Year(0))  +  "-"+ Cstr(docOptSetup.Billing_Month(0))
'		Call docthis.Save(True,False)
'		VerifyProcess = False
'		Exit Function
'	End If
	
	
'	If docthis.IsTwoReadingMeter(0) = "Yes" Then
'		excelPH = docthis.excelPH(0) 
'		excelOPH = docthis.excelOffPH(0) 
'		currentPH = Reading_PHCurrent
'		currentOPH = Reading_OffPHCurrent
'		If currentPH >excelPH Or currentOPH > excelOPH Then
'			docthis.BillGenStatus = "current reading is less " + Cstr(docOptSetup.Billing_Year(0))  +  "-"+ Cstr(docOptSetup.Billing_Month(0))
'			Call docthis.Save(True,False)
'			VerifyProcess = False
'			Exit Function
'		End If
'	Else
'		If docthis.txtcurrentreading(0) = "" Then
'			currentReading = 0
'		Else
'			currentReading = Cdbl(docthis.txtcurrentreading(0))
'		End If
'		If docthis.excel(0) = "" Then
'			excelReading = 0
'		Else
'			excelReading = Cdbl(docthis.excel(0))
'		End If
	
'		If currentReading > excelReading Then
'			docthis.BillGenStatus = "current reading is less " + Cstr(docOptSetup.Billing_Year(0))  +  "-"+ Cstr(docOptSetup.Billing_Month(0))
'			Call docthis.Save(True,False)
'			VerifyProcess = False
'			Exit Function
'		End If
'	End If
	
	VerifyProcess = True
End Function




Function PreviousBill() As Boolean
	
'	If session.CommonUserName = "Shahid Ghauri" Or session.CommonUserName = "Admin" Then
'		PreviousBill = True
'		Exit Function
'	End If
	
	
'	If docthis.NewConnection(0) = "Yes" Then
'		PreviousBill = True
'		Exit Function
'	End If
'	Msgbox docOptSetup.Billing_Month(0)
	
	If docOptSetup.Billing_Month(0) = "January" Then
		previousMonth = "2025" + "-" + "December"
		
	Elseif docOptSetup.Billing_Month(0) = "February" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "January"
		
	Elseif docOptSetup.Billing_Month(0) = "March" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "February"
		
	Elseif docOptSetup.Billing_Month(0) = "April" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "March"
		
	Elseif docOptSetup.Billing_Month(0) = "May" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "April"
		
	Elseif docOptSetup.Billing_Month(0) = "June" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "May"
		
	Elseif docOptSetup.Billing_Month(0) = "July" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "June"
		
	Elseif docOptSetup.Billing_Month(0) = "August" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "July"
		
	Elseif docOptSetup.Billing_Month(0) = "September" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "August"
		
	Elseif docOptSetup.Billing_Month(0) = "October" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "September"
		
	Elseif docOptSetup.Billing_Month(0) = "November" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "October"
		
	Elseif docOptSetup.Billing_Month(0) = "December" Then
		previousMonth = docOptSetup.Billing_Year(0) + "-" + "November"
		
	End If
	
	
	key = BTLC-29411 + "-" + "2024-September"
	Set dcPreviousBill = viewPreviousBill.GetAllDocumentsByKey(key,True)
	'Msgbox "previous bill key " + key + " Bill  " + Cstr(dcPreviousBill.Count)
	
	
	key  = docthis.REFRENCENOBARCODE(0) + "-" + previousMonth
	Set dcPreviousBill = viewPreviousBill.GetAllDocumentsByKey(key,True)
	
	'Msgbox "previous bill key " + key + " Bill  " + Cstr(dcPreviousBill.Count)
	
	If dcPreviousBill.Count  =  0 Then
		key  = docthis.refrencenobarcode(0)
		Set dcPreviousBill = viewPreviousBill.GetAllDocumentsByKey(key,False) 
		'Msgbox "Previous Month Bills " + Cstr(dcPreviousBill.Count)		
		If dcPreviousBill.Count = 0 Then
			PreviousBill = True
			Exit Function
		Else
			docthis.billgenstatus = "previous bill not exist"
			Call docthis.Save(True,False)
			billsummary = billsummary + " Previous Bill not found"
			Arrears = 0
			PreviousBill = False 
			Exit Function
		End If		
	End If	
	
	Set docPreviousBill = dcPreviousBill.GetFirstDocument
	If Cstr(docPreviousBill.amountduedate(0)) = "" Or Cstr(docPreviousBill.amountafterdate(0)) = "" Then
		docthis.billgenstatus = "wrong previous bill amount"
		Call docthis.Save(True,False)
		billsummary = billsummary + " wrong previous bill amount"
		Arrears = 0
		PreviousBill = False 
		Exit Function
	End If
	
	
	PreviousBill = True
	
	
End Function








Function GenerateBill()
	Call SetVariableZero()
	Call NewBill()
	Call SetPaymentStatus()
	Call AddMonthYear()
	Call AddCustomerDetail()
	Call RateAndServiceTax()
	Call AddFine()
	Call AddDefferedAmount()
	Call AddWaterCharges()
	'Call PreviousBill()
	Call AddInvoiceNo()
	Call AddValidDate()
	Call AddArrears()
	Call AddOthercharges()
	Call CalculateBill()
	
	
'	Call AddArrears()
'	Call AddExciseDuty()
'	Call AddFinancialCost()
'	Call AddFuelAdj()
'	Call AddPTVFee()
'	Call AddSalesTaxForRetailer()
'	Call AddGST()
'	Call AddFPA()
'	Call AddIncomeTax()
'	Call AddOPC()
'	Call AddExtraTax()
'	Call AddSalesTaxForRetailer()
'	
'	Call HistoryBilling()
	
'	docBill.gtotal = Cstr(CurrentBill)
'	docBill.billtotal = Cstr(CurrentBill)
'	docBill.total_units = Cstr(SumAmount)
'	docBill.totalUnits = Cstr(SumUnits)
'	'docBill.ct_mf = Cstr(MultiFactor)
'	docBill.exciseduty = Cstr(ExciseDuty)
'	TotalPayable = CurrentBill + Arrears
'	
	
	docthis.isgenerated = "1"
	docthis.generatedmonth = docOptSetup.Billing_Month(0) + "-" + docOptSetup.Billing_Year(0)
	docthis.BillGenStatus = "Generated"	
	docthis.txtpaymentstatus = "Unpaid"
	Call docBill.Save(True,False)
	Call docthis.Save(True,False)
	
	
'	docthis.BillGenStatus = "Generated"	
'	docthis.txtpaymentstatus = "Unpaid"
'	Call docBill.Save(True,False)
'	Call docthis.Save(True,False)
	
	
End Function

Function SetVariableZero()
	
	SumAmount = 0
	Tariff = ""
	Arrears = 0
	CurrentBill = 0
	TotalPayable = 0
	LPSurcharge = 0
	LatePaymentAmount = 0
	CurrentBill = 0
	PreviousCharges = 0
	newscharges = 0
	MaintCharges = 0
	
	AmountSurcharge = 0
	currentBill  = 0
	overallBill  = 0
	surcharge  = 0
	lateBill  = 0
End Function

Function NewBill()
	Set docBill = dbthis.Createdocument()
	'docBill.Form = "Generate Bill"
	docBill.Form = "Maintenance Bill"
	'docBill.Form = "TempBill"
End Function

Function SetPaymentStatus()
	docBill.paymentstatus = "unpaid"	
End Function

Function AddMonthYear()
	'Msgbox "Billing Year " + docOptSetup.Billing_Year(0) + "   Billing Month   "  + docOptSetup.Billing_Month(0)    
	docBill.Billing_Year = docOptSetup.Billing_Year(0)
	docBill.Billing_Month = docOptSetup.Billing_Month(0)
	docthis.Billing_Year = docOptSetup.Billing_Year(0)
	docthis.Billing_Month = docOptSetup.Billing_Month(0)
End Function

Function AddCustomerDetail()
	
	
	tariff = docthis.tariff(0)	
	docBill.refrencenobarcode = docthis.refrencenobarcode(0)
	docBill.NewCustomer_Refrence = docthis.NewCustomer_Refrence(0)
	docBill.Customer_Name =  docthis.Customer_Name(0)
	docBill.Plot_Number = docthis.Plot_Number(0)
	docBill.Meter_Number = docthis.Meter_Number(0)
	docBill.Sector = docthis.Sector(0)
	docBill.Block = docthis.Block(0)
	docBill.Plot_Size = docthis.Plot_Size(0)
	docBill.Feeder = docthis.Feeder(0)
	docBill.Meter_Type = docthis.Meter_Type(0)
	docBill.NIC_Number = docthis.NIC_Number(0)
	docBill.IncomeTaxWithHeld_Flag = docthis.IncomeTaxWithHeld_Flag(0)
	docBill.FurtherTax_Flag = docthis.FurtherTax_Flag(0)
	docBill.IncomeTaxWithHeld = Cstr(docthis.Arrears_IncomeTaxWithHeld(0))
	docBill.fpa1 = docthis.fpa1(0)
	docBill.fpa2 = docthis.fpa2(0)
	docBill.fpa3 = docthis.fpa3(0)
	docBill.totalFuelPrice = docthis.total_fueladj(0)
	docBill.tariff = docthis.tariff(0)
	docBill.ProjectName = docthis.ProjectName(0)
	docBill.customerno = docthis.customerno(0)
	docBill.barcode = docthis.barcode(0)
	
	If docthis.installedon(0) <> "" Then
		Dim dtInstalledOn As NotesDateTime
		Set dtInstalledOn = New NotesDateTime( docthis.installedon(0) )
		docBill.installedon = Format(dtInstalledOn.Dateonly, "dd-mmm-yyyy")
	End If
	
	
	docBill.txtunpaid = docthis.txtunpaid(0)
	docBill.Plot_Category = docthis.Plot_Category(0)
	docBill.Plot_Status = docthis.Plot_Status(0)
	docBill.txtlastmonthstatus = docthis.txtlastmonthstatus(0)
	docBill.txtlastmonthamount = docthis.txtlastmonthamount(0)
	docBill.txtlastmonthafterduedate = docthis.txtlastmonthafterduedate(0)
	docBill.IsTwoReadingMeter = docthis.IsTwoReadingMeter(0)
	docthis.BillGenStatus = "Generated-" + Cstr(docOptSetup.Billing_Year(0))  +  "-"+ Cstr(docOptSetup.Billing_Month(0))
	
	'Msgbox docOptSetup.Billing_Year(0)
	'Msgbox docOptSetup.reading_date(0)
	docBill.date_reading = docOptSetup.reading_date(0)
	docBill.date_issue = docOptSetup.date_issue(0)
	docBill.date_due = docOptSetup.date_due(0)
	docBill.method = "Multi"
	docthis.billgenerated = "1"
	TotalBills = TotalBills + 1
	Call docBill.Save(True,False)
	Call docthis.Save(True,False)
	
	
	
'	arrears = docCustomerProfile.maint_arrears(0)
'	lm_billingstatus = docCustomerProfile.maint_lastmonthstatus(0)
'	lm_amountafterduedate=docCustomerProfile.txtlastmonthafterduedate(0)
'	If lm_billingstatus="Unclear"   Then		
'		'' Change Due to Covid-19 Billing
'		lastmonthamount = docCustomerProfile.maint_afterduedate(0)		
'		''lastmonthamount = docCustomerProfile.maint_lastmonthamount(0)		
'		arrears =  lastmonthamount
'	End If
'	'Msgbox arrears
'	If docCustomerProfile.maint_paymentstatus(0)="Partially Paid"   Then
'		lastmonthamount = docCustomerProfile.maint_arrears(0)
'		arrears =  lastmonthamount
'		'Msgbox "1. Arrears : " +  Cstr(arrears)
'	End If
	
	docBill.Customer_Refrence = docthis.Customer_Refrence(0)	
	docBill.RefrenceNoBarCode = docthis.RefrenceNoBarCode(0)
	docBill.NewCustomer_Refrence = docthis.NewCustomer_Refrence(0)
	docBill.Customer_Name = docthis.Customer_Name(0)
	docBill.Plot_Number = docthis.Plot_Number(0)
	docBill.Street_Number = docthis.Street_Number(0)
	docBill.Block = docthis.Block(0)
	docBill.RefrenceNoBarCode = docthis.RefrenceNoBarCode(0)
	docBill.Plot_Size = docthis.Plot_Size(0)
	docBill.txtunpaid = docthis.txtunpaid(0)
	docBill.Plot_Category = docthis.Plot_Category(0)
	docBill.Plot_Status = docthis.Plot_Status(0)
	docBill.method = "multi"
	
	Call docBill.Save(True,False)
	
	
	
	'//////////////////////// Installment Amount /////////////////////////////////
'	installmentAmount = 0
'	If docthis.installmentamount1(0) <> "" And docthis.installmentamount1(0) <> "0"  Then
'		installmentAmount = docthis.installmentamount1(0)
'	Elseif  docthis.installmentamount2(0) <> "" And docthis.installmentamount2(0) <> "0"  Then
'		installmentAmount = docthis.installmentamount2(0)
'	Elseif  docthis.installmentamount3(0) <> "" And docthis.installmentamount3(0) <> "0"  Then
'		installmentAmount = docthis.installmentamount3(0)
'	Else
'		installmentAmount = 0
'	End If
'	docBill.installmentAmount = Cstr(installmentAmount)
'	'///////////////////////////////////////////////////////////////////////////////////////////////
'	docBill.Fine = Cstr(fine)
'	docBill.arrears = Cstr(arrears)
'	docBill.advance_payment  = docthis.maint_advance(0)
'	docBill.maint_previousarrears = docthis.maint_previousarrears(0)
'	docBill.watercharges  = docthis.maint_watercharges(0)
'	docBill.newscharges = docthis.maint_newscharges(0)
'	docBill.advancecharges = docthis.maint_advancecharges(0)
'	docBill.othercharges =  docthis.maint_othercharges(0)
'	maintenance=docBill.main_charges(0)
'	total= Clng(maintenance)
'	docBill.TotalMaintenanceCharges  = Cstr(total)
'	arrears=docBill.arrears(0) 
'	watercharges=docBill.watercharges(0)
'	newscharges=docBill.newscharges(0)
'	advancecharges=docBill.advancecharges(0)
'	othercharges=docBill.othercharges(0)
'	maint_previousarrears=docBill.maint_previousarrears(0)
'	'Msgbox arrears
	
	
	
	
	
	
'	gtotal=Clng(arrears) + Clng(maint_previousarrears) +  _ 
'	Clng(total) + Clng(watercharges) + Clng(newscharges) + _
'	Clng(advancecharges) + Clng(othercharges) + Clng(fine)   + _ 
'	Clng(ServiceTaxGovt)
'	
'	gtotal = gtotal + Clng(installmentAmount)
'	advance=docBill.txtadvance(0)
'	If advance = "" Then
'		advance = 0
'	End If
'	If (Clng(advance)  >  Clng(0)    And   Clng(advance) <  Clng(gtotal)) Then
'		gtotal = gtotal - advance
'	End If
'	docBill.advance_payment  = Cstr(advance)
'	docBill.GTotal = Cstr(gtotal)
'	docBill.amountduedate  = Cstr(gtotal)
'	surcharge = Round(Cdbl(docBill.TotalMaintenanceCharges(0)) * 10 / 100,0)
'	If(Clng(gtotal)<0) Then
'		surcharge=0
'	End If
'	amountafterdate = surcharge + gtotal
'	docBill.surcharge  = Cstr(surcharge)
'	docBill.amountafterdate  = Cstr(amountafterdate)
'	If  Cdbl(docBill.advance_payment(0)) > 0 Then
'		advanceamount = Cdbl(docBill.advance_payment(0)) 
'		If advanceamount > gtotal Then
'			advanceamount = advanceamount - gtotal
'			docBill.amountduedate = Cstr(0)
'			docBill.amountafterdate  = Cstr(0)
'			docBill.surcharge = Cstr(0)
'			docBill.advance_payment  = Cstr(advanceamount)
'		End If
''	End If
'	docBill.paymentstatus = "Unpaid"
'	docBill.readyforprint = "y"
'	Call docBill.Save(True,False)
	
	
'	advancePayment=Cdbl(docBill.advance_payment(0))
'	billTotal = Cdbl(docBill.GTotal(0))
'	'////////////////                                    IF ADVANCE GREATER THAN BILL GENERATED
'	If aDvancePayment >0 And aDvancePayment > billTotal  Then
'		aRrears=0
'		paymentStatus="paid"
'		unpaidCount =0
'		advancePayment = advancePayment -  billTotal 
'		billStatus="Clear" 
'		docBill.paymentstatus  = "Paid"
'	'////////////////                                  IF ADVANCE LESS THAN BILL GENERATED
'	Elseif adv_payment >0 And adv_payment < billTotal  Then
'		arrears=0		
'		paymentStatus="unpaid"	
'		unpaidCount =1
'		advancePayment=0
'		billStatus="Unclear" 
'		unpaidcount =1
'	Else
'	'////////////////                                    IF LAST MONTH BILL IS UNPAID
'		If docthis.maint_lastmonthstatus(0) = "Unclear" Then
'			lastmonthafterduedate = Round(docthis.maint_afterduedate(0),0)
'			lastmontharrears= Round(docthis.maint_arrears(0),0)
'			arrears =  Clng(lastmonthafterduedate )
'			paymentStatus="unpaid"
'			unpaidCount = docthis.maint_unpaidmonth(0)
'			uNpaidCount = Cint(uNpaidCount)  + 1
'			aDvancePayment=0
'			billStatus="Unclear" 
''		Elseif docthis.maint_lastmonthstatus(0) = "Partially Paid" Then
''			lastmonthafterduedate = Round(docthis.maint_afterduedate(0),0)
''			lastmontharrears= Round(docthis.maint_arrears(0),0)
''			arrears =  Clng(lastmonthafterduedate )
''			paymentStatus="unpaid"
''			unpaidCount = docthis.maint_unpaidmonth(0)
''			uNpaidCount = Cint(uNpaidCount)  + 1
''			aDvancePayment=0
''			billStatus="Unclear" 
'		Else
'			aRrears=0	
'			pAymentStatus="unpaid"
'			uNpaidCount =1
'			aDvancePayment=0
'			billStatus="Unclear" 
'		End If
'		
					'///////////////                                      IF LAST MONTH BILL IS PAID
'End If
	
	'///////////////////////////   Installment Updation in Customer Information Setup ///////////
'	If docthis.installmentamount1(0) <> ""   Then
'		docthis.installmentamount1 = ""
'	Elseif  docthis.installmentamount2(0) <> ""  Then
'		docthis.installmentamount2 = ""
'	Elseif  docthis.installmentamount3(0) <> ""   Then
'		docthis.TotalInstallments = ""
'		docthis.installmentamount3 = ""
'	End If
	'/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	
'	docthis.maint_arrears=Cstr(aRrears)
'	docthis.maint_paymentstatus=Cstr(pAymentStatus)
'	docthis.maint_unpaidmonth=Cstr(uNpaidCount)
'	docthis.txtadvance=Cstr(aDvancePayment	)
'	docthis.maint_lastmonthstatus=Cstr(billStatus)
'	docthis.maint_lastmonthamount=docBill.amountduedate(0)
'	docthis.maint_afterduedate=Cstr(Round(docBill.amountafterdate(0),0))
'	docthis.maint_lastbillingmonth=docBill.billing_month(0)
'	docthis.maint_lastbillingyear=docBill.billing_year(0)
'	docBill.Rep_outstandingwithsurcharge = Cstr(docBill.amountafterdate(0))
'	docthis.billgenerate_maintenance="y"
'	
'	Call docthis.Save(True,False)
End Function

Function RateAndServiceTax()
	category = docthis.plot_category(0)
	
	If Lcase(docthis.PlazaFloor(0)) = "plaza floor" Then
		Call RatesPlazaFloor()
		Exit Function
	End If
	
	If Lcase(docthis.Block(0)) = "awami apartments" Or _
	Lcase(docthis.Block(0)) = "awami apartment" Or _
	Lcase(docthis.Block(0)) = "apartments emc"   Then
		Call RatesApartments()
		Exit Function
	End If
	
	If  category = "Residential" Then
		Call RatesResidential()
		Exit Function
	Elseif category = "Commercial" Then
		Call RatesCommercial()
		Exit Function
	End If
	
	
	
	
	If docthis.plot_category(0)  = "Cinema" Then
		key = docthis.plot_category(0) + "-" + "1 M"
	Else
		key = docthis.plot_category(0) + "-" + docthis.plot_size(0)
	End If
	
	
'	If Instr(Lcase(docthis.Block(0)),"apartment") > 0  Then
'		key = "Apartment" + "-" + "1 M"
'	End If
	
	If Instr(Lcase(docthis.Block(0)),"commercial") > 0  Then
		key = "Commercial" + "-" + "1 M"		
		If  docthis.plot_size(0) <> "" Then
			key = docthis.plot_category(0) + "-" + docthis.plot_size(0)
		End If		
	End If
	
	Set dcRates = viewRates.GetAllDocumentsByKey(key,True)
	
	
	If dcRates.Count = 0 Then
	'	Msgbox "Charges not defined for plot size " + docthis.plot_size(0) + _
	'	+ " having category " + docthis.plot_category(0) + 	docthis.Maintenance_Charges(0)
		docthis.BillGenStatus = "rate not defined in rates setup"
		MaintCharges = Cdbl(docthis.maintenance_charges(0))
		ServiceTaxGovt = Cdbl(docthis.ServiceTaxGovt(0))
	Else
		Set docRates = dcRates.GetFirstDocument
		If Cstr(docRates.Rate(0)) = "" Then
			MaintCharges = 0
		Else
			MaintCharges = Cdbl(docRates.Rate(0))
		'	If docthis.Plot_Status(0) = "Under Construction" Then
		'		MaintCharges = MaintCharges * 75 / 100		
		'	End If
		End If		
		'Msgbox "Records  " + Cstr(dcRates.count) + "   Rate "   +   Cstr(docRates.Rate(0)) +  "         key " + key
		
		If Cstr(docRates.ServiceTaxGovt(0)) = "" Then
			ServiceTaxGovt = 0
		Else
			ServiceTaxGovt = Cdbl(docRates.ServiceTaxGovt(0))
		End If		
		
		
		If key = "Residential-5 M" Or key = "Residential-4 M"  Or  _
		key = "Residential-3 M" Or + _
		key = "Residential-1 M" Or key = "Residential-1 M" Then
			
		Else
			ServiceTaxGovt = Round(ServiceTaxGovt,-2)
		End If
		
	End If 
	
'	Msgbox docthis.MergingStatus(0)
	If docthis.MergingStatus(0) = "Merged" Then
		MaintCharges = MaintCharges / 2 
		MaintCharges = Round(MaintCharges,0)
	'	Msgbox MaintCharges
	End If
	
	'Msgbox docthis.plazafloor(0)
	If docthis.plazafloor(0) = "Plaza Floor" Then
		MaintCharges = Cdbl(docthis.Maintenance_Charges(0))
		ServiceTaxGovt = Cdbl(docthis.ServiceTaxGovt(0))
	End If
	
'	If docthis.RefrenceNoBarCode(0) = "BTL-28353" Then
'		MaintCharges = 1500
'		ServiceTaxGovt = 200
'	End If
	docBill.main_charges = Cstr(MaintCharges)
	docBill.ServiceTaxGovt  = Cstr(ServiceTaxGovt)
	'Msgbox "Maintenance Charge " + Cstr(MaintCharges) + "     Service Tax " + Cstr(ServiceTaxGovt)
End Function

Function AddFine()
	
	
	'Msgbox dbFine.FilePath
	Fine = 0
	Adjustment = 0
	WaivedOffAmount = 0
	Dim finedept As String
	Set viewFine = dbFine.GetView("KeyFineMaint")
	key = docthis.refrencenobarcode(0) + "-" + docthis.Billing_Month(0) + "-" + docthis.Billing_Year(0)
	Set dcFine = viewFine.GetAllDocumentsByKey(key,True)
	'Msgbox dcFine.Count
	'Msgbox key + Chr(10) + _
	'"Total Records " + Cstr(dcFine.Count)
	If dcFine.Count = 0  Then
		Fine = 0
	End If
	'Msgbox dcFine.Count
	
	If dcFine.Count > 0 Then
		Set docFine = dcFine.GetFirstDocument
		While Not docFine Is Nothing
			If Cstr(docFine.FineAmount(0)) = "" Then				
			Else
				
				If docFine.WaivedOffAmount(0) = "" Then
				Else
					WaivedOffAmount = WaivedOffAmount  + Cdbl(docFine.WaivedOffAmount(0) )
				End If
				finedept = docfine.Department(0)
				If finedept <> "" Then
					FineDept = "(" + FineDept  + ")"
				End If
				
				docBill.FineDept =  FineDept
				Fine = Fine + Cdbl(docFine.FineAmount(0))
			End If
			Set docFine = dcFine.GetNextDocument(docFine)
		Wend		
	End If
	
	
	'Set docFine = dcFine.GetFirstDocument
'	Msgbox Fine
'	Msgbox WaivedOffAmount
	
	
	
	Fine = Fine - WaivedOffAmount
	If Fine < 0 Then
		Fine = 0		
	End If
	
	
	If dcFine.Count > 0 Then
		Set docFine = dcFine.GetFirstDocument
		'Msgbox "Imran " +  docFine.Type(0)
		If docFine.Type(0) = "Adjustment" Then
			If docFine.AdjAmount(0) = "" Then
				adjustment = 0
			Else
				adjustment = Cdbl(docFine.AdjAmount(0))
			End If
			Fine = 0		
		End If
	End If
	
	
	'Msgbox Fine
'	If Trim(docthis.Maint_Fine(0)) = "" Then
'		Fine=0
'	Else
'		Fine = Cdbl(docthis.Maint_Fine(0))
'	End If		
'	Msgbox "Fine Amount " + Cstr(Fine)
	'Msgbox "Adjustment " + Cstr(Adjustment)
	
	docBill.fine = Fine
	docBill.adjustment = adjustment
	'docCustomerBill.Fine = Cstr(fine)
End Function


Function AddDefferedAmount()
	DefferedAmount = 0
	If docthis.DefferedAmount(0)  = "" Then
		DefferedAmount = 0
	Else
		DefferedAmount = Cdbl(docthis.DefferedAmount(0))
	End If
	docBill.DefferedAmount = Cstr(DefferedAmount)
End Function

Function AddWaterCharges()
	If Cstr(docthis.maint_watercharges(0)) = "" Then
		watercharges = 0
	Else
		watercharges = Cdbl(docthis.maint_watercharges(0) )
	End If
	If docthis.RefrenceNoBarCode(0) = "BTL-28349" Then
		watercharges = 50000
	End If
	docBill.watercharges = watercharges
	'Msgbox "Water Charges " + Cstr(watercharges)
End Function

Function AddInvoiceNo()
	If docthis.CustomerNo(0) = "" Then
		InvoiceNo = Cstr(InvoiceNoLeft) + "-"
	Else
		temp = Right(Cstr(docthis.CustomerNo(0)),5)
		InvoiceNo = Cstr(InvoiceNoLeft) + Cstr(temp)
	End If
'	Msgbox "Invoice No Left " + Cstr(InvoiceNoLeft)
'	Msgbox "Invoice No " + InvoiceNo
	docBill.InvoiceNo = Cstr(InvoiceNo)
End Function

Function AddValidDate()
	docBill.ValidDate = Format(ValidDate.DateOnly,"dd-mmm-yyyy")
	'Msgbox Format(ValidDate.DateOnly,"dd-mmm-yyyy")		
End Function

Function AddArrears()
	'Msgbox docPreviousBill.paymentstatus(0)
	If docPreviousBill Is Nothing Then
		Arrears = 0
	Else
		'Msgbox docPreviousBill.paymentstatus(0)
		If Lcase(docthis.txtpaymentstatus(0)) = "paid" Or Lcase(docthis.txtpaymentstatus(0)) = "paid with surcharge" Then
			Arrears = 0
		Elseif Lcase(docPreviousBill.paymentstatus(0)) = "unpaid" Or docPreviousBill.paymentstatus(0)  = ""  Then
			If Cstr(docPreviousBill.amountafterdate(0)) = "" Then
			Else
				Arrears = Arrears + Cdbl(docPreviousBill.amountafterdate(0))	
			End If			
		Elseif Lcase(docPreviousBill.paymentstatus(0)) = "partially paid" Then
			'Msgbox docPreviousBill.amountafterdate(0)
			'Msgbox docPreviousBill.paymentstatus(0)
			If docPreviousBill.paymentremaining(0) = "" Then
			Else
			'	Msgbox "Last Payment Status " + docPreviousBill.paymentremaining(0)
				Arrears = Arrears + Cdbl(docPreviousBill.paymentremaining(0))
			End If
			
		Else
			Arrears = 0
		End If
	End If
	'Msgbox "Arrears December-2023   " + Cstr(Arrears)
	'Msgbox docthis.main_arrears(0)
	
	If Cstr(docthis.main_arrears(0)) = "" Then
		Arrears = Arrears + 0
	Else
		Arrears  = Arrears + Cdbl(docthis.main_arrears(0))
	End If
	
	If Cstr(docthis.maint_arrears(0)) = "" Then
		Arrears = Arrears + 0
	Else
		Arrears  = Arrears + Cdbl(docthis.maint_arrears(0))
	End If
	
	
	'Arrears = 152020
	'Msgbox "Arrears  "  + Cstr(Arrears)
	docBill.arrears = Cstr(Arrears)
'	If docthis.arrears(0) = "" Then
'		Arrears = 0
'	Else
'		Arrears = Cdbl(docthis.arrears(0))
'	End If
	'Msgbox docthis.paymentstatus(0)
'	If Lcase(docthis.paymentstatus(0)) = "unpaid" Then
'		Arrears = Arrears + Cdbl(docthis.amountafterdate(0))
'	End If
	'Msgbox "Arrears   " + Cstr(Arrears)
	
End Function

Function AddOtherCharges()
	
	If Cstr(docthis.maint_othercharges(0)) = "" Then
		othercharges = 0
	Else
		othercharges = Cdbl(docthis.maint_othercharges(0))
	End If
	
	docBill.othercharges = Cstr(othercharges)
	
End Function

Function CalculateBill()
	currentBill = ServiceTaxGovt + Fine  +  previouscharges + _
	watercharges + newscharges  + MaintCharges  + othercharges
	
	currentBill = Round(currentBill, -1)
	
'	Msgbox "currentBill   " + Cstr(currentBill) + Chr(10) + _ 
'	"ServiceTaxGovt   " + Cstr(ServiceTaxGovt) + _
'	"Fine   " + Cstr(Fine) + _
'	"previouscharges   " + Cstr(previouscharges) +  Chr(10)  + _
'	"watercharges   " + Cstr(watercharges) +  Chr(10) + _
'	"newscharges   " + Cstr(newscharges)  +  Chr(10) + _
'	"MaintCharges   " + Cstr(MaintCharges)  + Chr(10) + _
'	"Other Charges " + Cstr(othercharges)
	
	docBill.TotalMaintenanceCharges = currentBill
'	Msgbox "Current Bill:  " + Cstr(currentBill) + _
'	"Arrears Bill:  " + Cstr(Arrears)
	
	AmountSurcharge = MaintCharges + ServiceTaxGovt
	OverallBill =    CurrentBill + Arrears
	OverallBill = Round(OverallBill,0)
	
	OverallBill = OverallBill - adjustment
	
'	Msgbox "currentBill   " + Cstr(currentBill) + Chr(10) + _ 
'	"OverallBill   " + Cstr(OverallBill)
	
	
'	Msgbox "SumAmount   " + Cstr(SumAmount) + Chr(10) + _ 
'	"ExciseDuty   " + Cstr(ExciseDuty) + Chr(10)  + _ 
'	"PTVFee   " + Cstr(PTVFee) + Chr(10) +  _ 
'	"GST   " + Cstr(GST) + Chr(10)  + _ 
'	"FinancialCost   " + Cstr(FinancialCost) + Chr(10) + _ 
'	"SumAQTA   " + Cstr(SumAQTA) + Chr(10) + _ 
'	"OPC   " + Cstr(OPC) + Chr(10) + _ 
'	"FPA   " + Cstr(FPA) +  _
'	"AmountSalesTaxForRetailer   " + Cstr(AmountSalesTaxForRetailer) + Chr(10) + _
'	"  MDIFixedCharges " + Cstr(MDIFixedCharges) +  Chr(10) + _ 
'	"  ExtraTax " + Cstr(ExtraTax) +  Chr(10) 
	
	
	
	If AmountSurcharge <= 0 Then
		Surcharge = 0
	Else
		Surcharge = AmountSurcharge * 10 / 100
		Surcharge = Round(Surcharge,0)
	End If
	
	'Msgbox "Overall Amount " + Cstr(OverallBill) + "    Surcharge " + Cstr(Surcharge) + "   Late Bill  " + Cstr(LateBill)
	If OverallBill < 0 Then
		Surcharge = 0
	End If
	
	
	
	LateBill = OverallBill  + Surcharge	
	docBill.TotalElecCharges = Cstr(CurrentBill)
	docBill.billtotal = Cstr(CurrentBill)
	
	docBill.surcharge = Cstr(Surcharge)
	docBill.amountduedate = Cstr(OverallBill)
	'Msgbox OverallBill
	docthis.amountduedate = Cstr(OverallBill)
	docBill.GTotal = Cstr(OverallBill)
	docBill.amountafterdate = Cstr(LateBill)
	
	
End Function




