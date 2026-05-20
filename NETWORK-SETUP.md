# Setup Network Access - SaungJajan

## 📱 Cara Akses dari Device Lain dalam Jaringan

### **1. Cek IP Address Server (Device Anda)**

**Windows:**
```powershell
ipconfig
```
Cari **IPv4 Address** di network adapter yang aktif (WiFi/Ethernet)
Contoh: `192.168.1.100`

### **2. Jalankan Aplikasi**

```powershell
dotnet run
```

Aplikasi akan listen di `http://0.0.0.0:5276` (semua network interfaces)

### **3. Akses dari Device Lain**

**Dari HP Penjual/Pembeli (dalam WiFi yang sama):**
```
http://192.168.1.100:5276
```

Ganti `192.168.1.100` dengan IP Address server Anda.

---

## 🔗 URL Akses

| Device | URL | Keterangan |
|--------|-----|------------|
| **Server (Local)** | `http://localhost:5276` | Dari device server |
| **HP Penjual** | `http://<IP-SERVER>:5276` | Dashboard Toko |
| **HP Pembeli** | `http://<IP-SERVER>:5276` | Menu/Keranjang |
| **Tablet Kiosk** | `http://<IP-SERVER>:5276` | Untuk antrian pembeli |

---

## 🔧 Konfigurasi Firewall (Jika Diperlukan)

Jika device lain tidak bisa akses, buka port di Windows Firewall:

**PowerShell (Run as Administrator):**
```powershell
New-NetFirewallRule -DisplayName "SaungJajan HTTP" -Direction Inbound -LocalPort 5276 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "SaungJajan HTTPS" -Direction Inbound -LocalPort 7077 -Protocol TCP -Action Allow
```

**Atau Manual:**
1. Buka **Windows Defender Firewall**
2. **Advanced Settings** → **Inbound Rules** → **New Rule**
3. **Port** → **TCP** → **Specific local ports: 5276, 7077**
4. **Allow the connection**
5. Beri nama: "SaungJajan ASP.NET"

---

## 📋 Langkah Setup Database

### **Database di Server (localhost):**

File `appsettings.json` sudah terkonfigurasi:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=db_sajan;User=root;Password=;"
  }
}
```

**Jika database di device lain:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.100;Port=3306;Database=db_sajan;User=root;Password=yourpassword;"
  }
}
```

---

## 🚀 Quick Start Script

Simpan sebagai `start-server.ps1`:

```powershell
# Get IP Address
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" -and $_.IPAddress -like "192.168.*" } | Select-Object -First 1).IPAddress

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  SaungJajan Server Starting..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Local Access:  http://localhost:5276" -ForegroundColor Green
Write-Host "Network Access: http://$ip`:5276" -ForegroundColor Yellow
Write-Host ""
Write-Host "Share this URL with sellers/buyers on the same network:" -ForegroundColor White
Write-Host "http://$ip`:5276" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop server" -ForegroundColor Gray
Write-Host ""

dotnet run
```

**Jalankan:**
```powershell
.\start-server.ps1
```

---

## 🔒 Security Notes

### **Untuk Development/Local Network:**
✅ Gunakan dalam jaringan WiFi yang terpercaya  
✅ Password database sebaiknya diisi  
✅ Session timeout akan logout otomatis  

### **Untuk Production/Public:**
⚠️ Gunakan HTTPS (SSL Certificate)  
⚠️ Implementasi proper authentication (password untuk toko)  
⚠️ Gunakan environment variables untuk connection string  
⚠️ Setup rate limiting & input validation  

---

## 📊 Contoh Skenario Penggunaan

### **Skenario 1: UMKM di Warung**
```
Server (Laptop Kasir):
├─ Running SaungJajan
├─ Database MySQL
└─ IP: 192.168.1.100

Penjual (HP):
└─ Akses: http://192.168.1.100:5276/Auth/LoginToko
   └─ Dashboard untuk lihat pesanan & kelola produk

Pembeli (HP/Tablet):
└─ Akses: http://192.168.1.100:5276
   └─ Login → Pilih warung → Order → Checkout
```

### **Skenario 2: Food Court dengan Kiosk**
```
Server (PC Admin):
└─ IP: 192.168.1.50

Multiple Penjual (HP masing-masing):
├─ Penjual A: http://192.168.1.50:5276/Auth/LoginToko
├─ Penjual B: http://192.168.1.50:5276/Auth/LoginToko
└─ Penjual C: http://192.168.1.50:5276/Auth/LoginToko

Kiosk Tablet (Antrian Pembeli):
└─ http://192.168.1.50:5276
   └─ Pembeli 1 login → order → logout
   └─ Pembeli 2 login → order → logout
```

---

## 🐛 Troubleshooting

### **Device lain tidak bisa akses:**
1. ✅ Pastikan dalam **WiFi/network yang sama**
2. ✅ Cek **Firewall** Windows (buka port 5276)
3. ✅ Verify IP Address server benar
4. ✅ Test ping: `ping 192.168.1.100`

### **Database connection error:**
1. ✅ MySQL service running?
2. ✅ Port 3306 accessible?
3. ✅ Username/password benar?
4. ✅ Database `db_sajan` sudah dibuat?

### **Aplikasi tidak start:**
```powershell
# Check if port already in use
netstat -ano | findstr :5276

# Kill process if needed
taskkill /PID <PID> /F
```

---

## 📞 Support

Jika ada masalah dengan network setup, pastikan:
- Semua device dalam **network yang sama**
- **Firewall** sudah dikonfigurasi
- **IP Address** server statis (tidak berubah)

Untuk set IP statis di Windows:
```
Settings → Network & Internet → WiFi/Ethernet → Edit IP assignment → Manual
```
