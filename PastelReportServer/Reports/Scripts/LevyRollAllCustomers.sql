select m.CustomerCode, m.CustomerDesc,
  BalanceLast01, BalanceLast02 ,BalanceLast03 ,BalanceLast04 ,BalanceLast05 ,BalanceLast06, BalanceLast07, BalanceLast08, BalanceLast09, BalanceLast10, BalanceLast11,BalanceLast12,BalanceLast13,
  BalanceThis01,BalanceThis02, BalanceThis03, BalanceThis04, BalanceThis05, BalanceThis06, BalanceThis07, BalanceThis08, BalanceThis09, BalanceThis10, BalanceThis11, BalanceThis12,
  0.0 as Levy,
  0.0 as Water,
  0.0 as Electricity ,
  0.0 as Interest ,
  0.0 as Legal,
  0.0 as Sewer,
  0.0 as Payments,
  0.0 as Sundries,
  0.0 as CSOS
from  [DataSet].CustomerMaster m 
group by  m.CustomerCode, m.CustomerDesc, BalanceThis01,
  BalanceThis01,BalanceThis02, BalanceThis03, BalanceThis04, BalanceThis05, BalanceThis06, BalanceThis07, BalanceThis08, BalanceThis09, BalanceThis10, BalanceThis11, BalanceThis12,
  BalanceLast01, BalanceLast02 ,BalanceLast03 ,BalanceLast04 ,BalanceLast05 ,BalanceLast06, BalanceLast07, BalanceLast08, BalanceLast09, BalanceLast10, BalanceLast11,BalanceLast12,BalanceLast13