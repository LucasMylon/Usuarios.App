$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
	throw 'Docker não encontrado. Instale e inicie o Docker Desktop.'
}

docker info --format '{{.ServerVersion}}' | Out-Null
if ($LASTEXITCODE -ne 0) {
	throw 'Não foi possível acessar o Docker. Inicie o Docker Desktop e tente novamente.'
}

$envFile = Join-Path $PSScriptRoot '.env.demo'
if (-not (Test-Path -LiteralPath $envFile)) {
	function New-RandomBytes([int]$count) {
		$bytes = New-Object byte[] $count
		$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
		try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
		return ,$bytes
	}
	$sqlPassword = 'Aa1!' + [BitConverter]::ToString((New-RandomBytes 24)).Replace('-', '')
	$rabbitPassword = [BitConverter]::ToString((New-RandomBytes 24)).Replace('-', '')
	$jwtKey = [Convert]::ToBase64String((New-RandomBytes 32))
	@(
		"DEMO_SQL_PASSWORD=$sqlPassword"
		"DEMO_RABBIT_PASSWORD=$rabbitPassword"
		"DEMO_JWT_KEY=$jwtKey"
	) | Set-Content -LiteralPath $envFile -Encoding ASCII
	Write-Host 'Credenciais locais geradas em .env.demo. Guarde esse arquivo enquanto usar os volumes.'
}

docker compose --env-file .env.demo -f docker-compose.demo.yml up --build -d
if ($LASTEXITCODE -ne 0) {
	throw 'Não foi possível iniciar a demonstração. Consulte as mensagens acima.'
}

$ready = $false
for ($attempt = 0; $attempt -lt 60; $attempt++) {
	try {
		$null = Invoke-WebRequest -UseBasicParsing 'http://localhost:5237/swagger/index.html' -TimeoutSec 3
		$ready = $true
		break
	}
	catch {
		Start-Sleep -Seconds 2
	}
}
if (-not $ready) {
	throw 'A API não ficou pronta. Consulte: docker compose --env-file .env.demo -f docker-compose.demo.yml logs api'
}
Write-Host 'API: http://localhost:5237/swagger'
Write-Host 'Caixa de e-mails de teste: http://localhost:8026'
