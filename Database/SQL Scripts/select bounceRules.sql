/**********************************************
MANTA - Select info about Bounce Rules
***********************************************/

select
	rle.evn_bounceRule_id,
	rle.evn_bounceRule_name,
	rle.evn_bounceRule_executionOrder,
	rct.evn_bounceRuleCriteriaType_name,
	rle.evn_bounceRule_criteria,
	CONVERT(NVARCHAR, rle.evn_bounceRule_mantaBounceType) + ') ' + bnt.evn_bounceType_name,
	CONVERT(NVARCHAR, rle.evn_bounceRule_mantaBounceCode) + ') ' + bnc.evn_bounceCode_name	
from
	man_evn_bounceRule as rle
	left join man_evn_bounceRuleCriteriaType as rct
	on rle.evn_bounceRuleCriteriaType_id = rct.evn_bounceRuleCriteriaType_id
	left join man_evn_bounceType as bnt
	on rle.evn_bounceRule_mantaBounceType = bnt.evn_bounceType_id
	left join man_evn_bounceCode as bnc
	on rle.evn_bounceRule_mantaBounceCode = bnc.evn_bounceCode_id
order by
	rle.evn_bounceRule_executionOrder