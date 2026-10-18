function Get-NextRevision([string]$Current) {
 if($Current -notmatch '^(0|[1-9][0-9]*)\.(0|[1-9]|1[0-2])\.(0|[1-9]|1[0-2])$'){throw 'Invalid revision: '+$Current}
 $parts=@($Current.Split('.') | ForEach-Object { [int]$_ })
 $parts[2]++
 if($parts[2] -gt 12){$parts[2]=0;$parts[1]++}
 if($parts[1] -gt 12){$parts[1]=0;$parts[0]++}
 return ($parts -join '.')
}
