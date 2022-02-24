import React, {useMemo, useState} from 'react';
import {observer} from 'mobx-react-lite';
import {DefaultButton, PrimaryButton, TextField} from "office-ui-fabric-react";
import {ScheduleSelect} from "./ScheduleSelect";
import {useHistory, useParams} from "react-router";
import {useTranslation} from "react-i18next";
import {Schedule} from "../Utilities/DataTypes/Schedule";
import Creatable from "react-select/creatable";
import {appState} from "../App";
import {ScheduleDeleteModal} from "./ScheduleDeleteModal";
import {Cron} from "../Utilities/DataTypes/Cron";
import Select from "react-select";
import dayjs from "dayjs";
import {SimpleDate} from "./SimpleDate";

export const SchedulePanel = observer(({schedule}: {schedule: Schedule})=>{
    const { t } = useTranslation();
    const history = useHistory();
    const {projectId} = useParams();
    const [showDeleteModal, setShowDeleteModal] = useState(false)

    function saveChanges(){
        schedule.saveChanges()
            .then(()=>history.push(`/projects/${projectId}`))
    }

    const timeZones = useMemo(()=>{
        return appState.timezones.map(val=>({value: val, label: val}))
    }, [])

    return (
        <div id={"schedulePanel"}>
            <TextField
                label={t("Schedule Name")}
                placeholder={t("Schedule Name")}
                value={schedule.name}
                onChange={((event, newValue) => schedule.setName(newValue || ""))}
            />
            <header>
                <div style={{flex: 1, display: 'flex'}}>{t("Last Run")}: {schedule.lastStart ? <SimpleDate date={dayjs(schedule.lastStart)}/> : `- ${t("Has Not Run")} -`}</div>
                <div style={{flex: 1}}>{t("Last Converted File Count")}: {schedule.lastFileCount}</div>
            </header>

            <hr/>

            <label htmlFor={"ifcSettings"}>{t("IFC Settings Set Name in Revit")}</label>
            <Creatable
                name={"ifcSettings"}
                options={appState.ifcSettings.map(val=>val.listValue)}
                value={schedule.ifcSettingsName ? {label: schedule.ifcSettingsName, value: schedule.ifcSettingsName} : undefined}
                onCreateOption={(value)=>schedule.setIfcSettingsName(value)}
                onChange={(option)=>{
                    //@ts-ignore -- value is a valid option for OptionType
                    schedule.setIfcSettingsName(option?.value || "");
                }}
            />

            <hr/>

            <ScheduleSelect cron={schedule.cron}/>


            <Select
                label={"Time Zone"}
                value={timeZones.find(val=>val.value === schedule.timeZoneId)}
                options={timeZones}
                onChange={(item: any)=>schedule.setTimeZoneId(item.value)}
            />

            <div className={"buttonTray"}>
                <PrimaryButton text={t("Save Changes")} disabled={!schedule.hasChanged || !Cron.IsValid(schedule.cron.toString())}
                               onClick={saveChanges}/>
                <div style={{flex: 1}}/>
                <DefaultButton text={t("Cancel")} onClick={()=>history.push(`/projects/${projectId}`)}/>
            </div>
            <PrimaryButton style={{marginTop: '10px'}} className={"deleteButton"} text={t("Delete Schedule")} onClick={()=>setShowDeleteModal(true)}/>

            <ScheduleDeleteModal schedule={schedule} show={showDeleteModal} setShow={setShowDeleteModal}/>
        </div>
    )
})