"""Generate sample banking PDFs for the Content Understanding demo."""
from fpdf import FPDF
import random
import os

os.makedirs('assets', exist_ok=True)

# Fake data pools
first_names = ['James','Maria','Robert','Sarah','Michael','Jennifer','David','Linda',
               'William','Patricia','Carlos','Aisha','Wei','Fatima','Aleksandr']
last_names = ['Thompson','Garcia','Chen','Williams','Patel','Johnson','Kim','Martinez',
              'Anderson','Singh','Nakamura','OBrien','Hassan','Mueller','Petrov']
streets = ['123 Oak Lane','456 Maple Ave','789 Pine St','1010 Cedar Blvd',
           '2020 Elm Dr','555 Birch Way','777 Walnut Ct','300 Spruce Rd']
cities = [('Austin','TX','78701'),('Denver','CO','80202'),('Portland','OR','97201'),
          ('Seattle','WA','98101'),('Chicago','IL','60601'),('Boston','MA','02101'),
          ('Miami','FL','33101'),('Atlanta','GA','30301')]
employers = ['Meridian Tech Solutions','Pacific Northwest Credit Union',
             'Bayshore Financial Group','Summit Healthcare Systems',
             'Atlas Manufacturing Inc','Greenfield Consulting LLC',
             'Horizon Energy Corp','Vertex Digital Media']
utilities = ['Pacific Gas & Electric','Consolidated Edison','Duke Energy',
             'National Grid','Xcel Energy','Southern California Edison']
dobs = ['1985-03-15','1990-07-22','1978-11-03','1992-01-28','1983-06-10',
        '1995-09-17','1970-12-05','1988-04-19','1976-08-30','1993-02-14']
account_types = ['Individual Checking','Joint Checking','Business Checking',
                 'Premium Savings','Money Market','Certificate of Deposit']


def doc_number():
    letter = random.choice('ABCDEFGHJKLMNPRSTUVWXYZ')
    num = random.randint(10000000, 99999999)
    return f"{letter}{num}"


def ssn_masked():
    return f"***-**-{random.randint(1000, 9999)}"


docs = []

# --- 5 Driver's Licenses ---
for i in range(5):
    fn = random.choice(first_names)
    ln = random.choice(last_names)
    city, state, zipcode = random.choice(cities)
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 18)
    pdf.cell(0, 12, f"STATE OF {state} - DRIVER LICENSE", new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.ln(5)
    pdf.set_font('Helvetica', '', 12)
    pdf.cell(0, 8, f"License No: {doc_number()}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Name: {fn} {ln}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Date of Birth: {random.choice(dobs)}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Address: {random.choice(streets)}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"City/State/Zip: {city}, {state} {zipcode}", new_x="LMARGIN", new_y="NEXT")
    sex = random.choice(["M", "F"])
    eyes = random.choice(["BRN", "BLU", "GRN", "HZL"])
    ht_ft = random.randint(5, 6)
    ht_in = random.randint(0, 11)
    pdf.cell(0, 8, f"Sex: {sex}  Eyes: {eyes}  Ht: {ht_ft}'{ht_in}\"", new_x="LMARGIN", new_y="NEXT")
    dl_class = random.choice(["C", "D"])
    issued = f"2023-{random.randint(1,12):02d}-{random.randint(1,28):02d}"
    expires = f"2029-{random.randint(1,12):02d}-{random.randint(1,28):02d}"
    pdf.cell(0, 8, f"Class: {dl_class}  Issued: {issued}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Expires: {expires}", new_x="LMARGIN", new_y="NEXT")
    fname = f"assets/drivers_license_{i+1:02d}_{ln.lower()}.pdf"
    pdf.output(fname)
    docs.append(fname)

# --- 3 Passports ---
for i in range(3):
    fn = random.choice(first_names)
    ln = random.choice(last_names)
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 20)
    pdf.cell(0, 15, "UNITED STATES OF AMERICA", new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.set_font('Helvetica', 'B', 16)
    pdf.cell(0, 10, "PASSPORT", new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.ln(8)
    pdf.set_font('Helvetica', '', 12)
    pdf.cell(0, 8, f"Passport No: {doc_number()}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Surname: {ln}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Given Names: {fn}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, "Nationality: United States", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Date of Birth: {random.choice(dobs)}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Sex: {random.choice(['M','F'])}", new_x="LMARGIN", new_y="NEXT")
    birth_city = random.choice(cities)
    pdf.cell(0, 8, f"Place of Birth: {birth_city[0]}, {birth_city[1]}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Date of Issue: 2022-{random.randint(1,12):02d}-{random.randint(1,28):02d}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Date of Expiration: 2032-{random.randint(1,12):02d}-{random.randint(1,28):02d}", new_x="LMARGIN", new_y="NEXT")
    fname = f"assets/passport_{i+1:02d}_{ln.lower()}.pdf"
    pdf.output(fname)
    docs.append(fname)

# --- 4 Utility Bills (Proof of Address) ---
for i in range(4):
    fn = random.choice(first_names)
    ln = random.choice(last_names)
    city, state, zipcode = random.choice(cities)
    utility = random.choice(utilities)
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 16)
    pdf.cell(0, 12, utility, new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.set_font('Helvetica', '', 10)
    pdf.cell(0, 6, "Monthly Statement", new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.ln(8)
    pdf.set_font('Helvetica', '', 12)
    pdf.cell(0, 8, f"Account Holder: {fn} {ln}", new_x="LMARGIN", new_y="NEXT")
    street = random.choice(streets)
    pdf.cell(0, 8, f"Service Address: {street}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"                 {city}, {state} {zipcode}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 8, f"Account Number: {random.randint(100000000, 999999999)}", new_x="LMARGIN", new_y="NEXT")
    stmt_month = random.randint(1, 4)
    stmt_day = random.randint(1, 28)
    pdf.cell(0, 8, f"Statement Date: 2026-0{stmt_month}-{stmt_day:02d}", new_x="LMARGIN", new_y="NEXT")
    amount = random.uniform(45, 320)
    pdf.cell(0, 8, f"Amount Due: ${amount:.2f}", new_x="LMARGIN", new_y="NEXT")
    due_month = random.randint(2, 5)
    due_day = random.randint(1, 28)
    pdf.cell(0, 8, f"Due Date: 2026-0{due_month}-{due_day:02d}", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(5)
    pdf.set_font('Helvetica', 'B', 11)
    pdf.cell(0, 8, "Usage Summary", new_x="LMARGIN", new_y="NEXT")
    pdf.set_font('Helvetica', '', 11)
    prev_reading = random.randint(10000, 50000)
    curr_reading = random.randint(50001, 60000)
    usage = curr_reading - prev_reading
    pdf.cell(0, 7, f"Previous Reading: {prev_reading} kWh", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Current Reading: {curr_reading} kWh", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Usage This Period: {usage} kWh", new_x="LMARGIN", new_y="NEXT")
    fname = f"assets/utility_bill_{i+1:02d}_{ln.lower()}.pdf"
    pdf.output(fname)
    docs.append(fname)

# --- 5 Account Application Forms ---
for i in range(5):
    fn = random.choice(first_names)
    ln = random.choice(last_names)
    city, state, zipcode = random.choice(cities)
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 18)
    pdf.cell(0, 12, "FIRST NATIONAL BANK", new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.set_font('Helvetica', 'B', 14)
    pdf.cell(0, 10, "New Account Application", new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.ln(5)
    pdf.set_font('Helvetica', 'B', 11)
    pdf.cell(0, 8, "SECTION 1: PERSONAL INFORMATION", new_x="LMARGIN", new_y="NEXT")
    pdf.set_font('Helvetica', '', 11)
    pdf.cell(0, 7, f"Full Legal Name: {fn} {ln}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Date of Birth: {random.choice(dobs)}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"SSN: {ssn_masked()}", new_x="LMARGIN", new_y="NEXT")
    area = random.randint(200, 999)
    prefix = random.randint(200, 999)
    line = random.randint(1000, 9999)
    pdf.cell(0, 7, f"Phone: ({area}) {prefix}-{line}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Email: {fn.lower()}.{ln.lower()}@email.com", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(3)
    pdf.set_font('Helvetica', 'B', 11)
    pdf.cell(0, 8, "SECTION 2: ADDRESS", new_x="LMARGIN", new_y="NEXT")
    pdf.set_font('Helvetica', '', 11)
    pdf.cell(0, 7, f"Street: {random.choice(streets)}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"City: {city}  State: {state}  Zip: {zipcode}", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(3)
    pdf.set_font('Helvetica', 'B', 11)
    pdf.cell(0, 8, "SECTION 3: EMPLOYMENT", new_x="LMARGIN", new_y="NEXT")
    pdf.set_font('Helvetica', '', 11)
    pdf.cell(0, 7, f"Employer: {random.choice(employers)}", new_x="LMARGIN", new_y="NEXT")
    income = random.randint(45, 185) * 1000
    pdf.cell(0, 7, f"Annual Income: ${income:,}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Years Employed: {random.randint(1, 15)}", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(3)
    pdf.set_font('Helvetica', 'B', 11)
    pdf.cell(0, 8, "SECTION 4: ACCOUNT REQUEST", new_x="LMARGIN", new_y="NEXT")
    pdf.set_font('Helvetica', '', 11)
    acct_type = random.choice(account_types)
    deposit = random.randint(500, 25000)
    purpose = random.choice(["Personal banking", "Payroll deposit", "Business operations",
                             "Savings goal", "Investment"])
    pdf.cell(0, 7, f"Account Type: {acct_type}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Initial Deposit: ${deposit:,}.00", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Purpose: {purpose}", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(5)
    pdf.set_font('Helvetica', 'I', 9)
    pdf.multi_cell(0, 5, "I certify that the information provided is true and complete. "
                   "I authorize First National Bank to verify any information provided "
                   "and to obtain credit reports as necessary.")
    pdf.ln(8)
    pdf.set_font('Helvetica', '', 11)
    sign_date = f"2026-0{random.randint(1,4)}-{random.randint(1,28):02d}"
    pdf.cell(0, 7, f"Signature: ________________________  Date: {sign_date}", new_x="LMARGIN", new_y="NEXT")
    fname = f"assets/account_application_{i+1:02d}_{ln.lower()}.pdf"
    pdf.output(fname)
    docs.append(fname)

# --- 2 Bank Statements (Proof of Address alternative) ---
for i in range(2):
    fn = random.choice(first_names)
    ln = random.choice(last_names)
    city, state, zipcode = random.choice(cities)
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 16)
    pdf.cell(0, 12, "WESTERN SAVINGS BANK", new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.set_font('Helvetica', '', 10)
    pdf.cell(0, 6, "Monthly Account Statement", new_x="LMARGIN", new_y="NEXT", align='C')
    pdf.ln(6)
    pdf.set_font('Helvetica', '', 11)
    pdf.cell(0, 7, f"Account Holder: {fn} {ln}", new_x="LMARGIN", new_y="NEXT")
    street = random.choice(streets)
    pdf.cell(0, 7, f"Address: {street}, {city}, {state} {zipcode}", new_x="LMARGIN", new_y="NEXT")
    acct_last4 = random.randint(1000, 9999)
    pdf.cell(0, 7, f"Account: ****{acct_last4}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, "Statement Period: March 1 - March 31, 2026", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(5)
    pdf.set_font('Helvetica', 'B', 11)
    pdf.cell(0, 8, "ACCOUNT SUMMARY", new_x="LMARGIN", new_y="NEXT")
    pdf.set_font('Helvetica', '', 11)
    opening = random.uniform(2000, 50000)
    deposits = random.uniform(1000, 8000)
    withdrawals = random.uniform(500, 4000)
    closing = opening + deposits - withdrawals
    pdf.cell(0, 7, f"Opening Balance: ${opening:,.2f}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Total Deposits: ${deposits:,.2f}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Total Withdrawals: ${withdrawals:,.2f}", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 7, f"Closing Balance: ${closing:,.2f}", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(5)
    pdf.set_font('Helvetica', 'B', 11)
    pdf.cell(0, 8, "RECENT TRANSACTIONS", new_x="LMARGIN", new_y="NEXT")
    pdf.set_font('Helvetica', '', 10)
    txns = ['Direct Deposit - Payroll', 'ATM Withdrawal', 'Online Transfer',
            'Debit Card - Grocery', 'Check #1042', 'ACH Payment - Rent', 'Utility Payment']
    for t in random.sample(txns, 5):
        amt = random.uniform(-2000, 5000)
        day = random.randint(1, 28)
        pdf.cell(0, 6, f"  03/{day:02d}  {t:<40s} ${amt:>10,.2f}", new_x="LMARGIN", new_y="NEXT")
    fname = f"assets/bank_statement_{i+1:02d}_{ln.lower()}.pdf"
    pdf.output(fname)
    docs.append(fname)

# --- 1 W-2 Tax Form (additional identity/income verification) ---
fn = random.choice(first_names)
ln = random.choice(last_names)
city, state, zipcode = random.choice(cities)
pdf = FPDF()
pdf.add_page()
pdf.set_font('Helvetica', 'B', 14)
pdf.cell(0, 10, "Form W-2  Wage and Tax Statement  2025", new_x="LMARGIN", new_y="NEXT", align='C')
pdf.ln(8)
pdf.set_font('Helvetica', '', 11)
employer = random.choice(employers)
pdf.cell(0, 7, f"Employer: {employer}", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Employer EIN: {random.randint(10,99)}-{random.randint(1000000,9999999)}", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Employee: {fn} {ln}", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"SSN: {ssn_masked()}", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Address: {random.choice(streets)}, {city}, {state} {zipcode}", new_x="LMARGIN", new_y="NEXT")
pdf.ln(5)
wages = random.randint(45000, 185000)
fed_tax = int(wages * random.uniform(0.18, 0.28))
ss_wages = min(wages, 160200)
ss_tax = int(ss_wages * 0.062)
med_wages = wages
med_tax = int(med_wages * 0.0145)
pdf.cell(0, 7, f"Box 1 - Wages: ${wages:,}.00", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Box 2 - Federal Tax Withheld: ${fed_tax:,}.00", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Box 3 - Social Security Wages: ${ss_wages:,}.00", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Box 4 - Social Security Tax: ${ss_tax:,}.00", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Box 5 - Medicare Wages: ${med_wages:,}.00", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Box 6 - Medicare Tax: ${med_tax:,}.00", new_x="LMARGIN", new_y="NEXT")
state_wages = wages
state_tax = int(state_wages * random.uniform(0.03, 0.09))
pdf.cell(0, 7, f"Box 16 - State Wages: ${state_wages:,}.00", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 7, f"Box 17 - State Tax: ${state_tax:,}.00", new_x="LMARGIN", new_y="NEXT")
fname = f"assets/w2_form_01_{ln.lower()}.pdf"
pdf.output(fname)
docs.append(fname)

print(f"\nGenerated {len(docs)} PDFs in assets/:")
for d in sorted(docs):
    print(f"  {d}")
